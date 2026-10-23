using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using SaphMovel.Configuration;
using SaphMovel.Services.Voice;
using SaphMovel.ViewModels;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace SaphMovel.Platforms.Android.Voice;

/// <summary>Vosk offline + microfone; extrai «olá sistema» + comando e delega ao menu STATUS.</summary>
public sealed class VoskListeningService : IVoskListeningService
{
	readonly ILogger<VoskListeningService> _log;
	readonly object _runGate = new();

	/// <summary>Serializa <see cref="StartAsync"/> / <see cref="StopAsync"/> (modais abrem/fecham rápido, gravador retoma a escuta).</summary>
	readonly SemaphoreSlim _lifecycle = new(1, 1);

	CancellationTokenSource? _cts;
	Task? _loopTask;
	VoiceWakeSession? _session;
	MainViewModel? _viewModel;

	/// <summary>Incrementa a cada arranque — o laço só limpa o estado se ainda for o da sua geração.</summary>
	int _generation;

	/// <summary>
	/// org.vosk.Model carregado uma única vez: recriá-lo a cada volta à página principal demorava segundos
	/// e a escuta ficava surda nesse intervalo.
	/// </summary>
	IntPtr _model;
	string? _modelDir;

	/// <summary>Reabre o microfone se só chegarem zeros (captura silenciada pelo Android) durante este tempo.</summary>
	static readonly TimeSpan DigitalSilenceTimeout = TimeSpan.FromSeconds(6);

	const string MicReconnectHint = "Voz: a reconectar o microfone…";

	/// <summary>Sobrevive ao <see cref="StopAsync"/> para permitir <see cref="ResumeAsync"/> (pausa por gravação/TTS).</summary>
	MainViewModel? _lastViewModel;

	public event EventHandler<bool>? ListeningActiveChanged;

	public VoskListeningService(ILogger<VoskListeningService> log) => _log = log;

	void RaiseListeningActive(bool active)
	{
		MainThread.BeginInvokeOnMainThread(() =>
			ListeningActiveChanged?.Invoke(this, active));
	}

	public bool IsListening
	{
		get
		{
			lock (_runGate)
				return _cts is not null;
		}
	}

	// Copiam o ViewModel para uma local: o lambda corre mais tarde e o StopAsync pode já ter limpo o campo.
	void VmHint(string? message)
	{
		var vm = CurrentViewModel();
		if (vm is null)
			return;
		MainThread.BeginInvokeOnMainThread(() => vm.SetVoiceOfflineHint(message));
	}

	void VmModelReadyHint(string? message)
	{
		var vm = CurrentViewModel();
		if (vm is null)
			return;
		MainThread.BeginInvokeOnMainThread(() => vm.SetVoiceModelReadyHint(message));
	}

	void VmLastHeard(string? text)
	{
		var vm = CurrentViewModel();
		if (vm is null)
			return;
		MainThread.BeginInvokeOnMainThread(() => vm.SetLastVoiceHeardText(text));
	}

	static void HintTo(MainViewModel vm, string? message) =>
		MainThread.BeginInvokeOnMainThread(() => vm.SetVoiceOfflineHint(message));

	public async Task StartAsync(MainViewModel viewModel, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(viewModel);
		await _lifecycle.WaitAsync().ConfigureAwait(false);
		try
		{
			lock (_runGate)
			{
				if (_cts is not null)
					return;
				_lastViewModel = viewModel;
			}

			// Android só mostra o diálogo de permissão quando o pedido é feito na thread principal (Activity).
			var status = await MainThread.InvokeOnMainThreadAsync(() =>
				Permissions.RequestAsync<Permissions.Microphone>());
			if (status != PermissionStatus.Granted)
			{
				_log.LogWarning("Vosk: permissão de microfone negada.");
				HintTo(viewModel, "Voz offline: conceda permissão de microfone nas definições da app para ser ouvido.");
				RaiseListeningActive(false);
				return;
			}

			var (modelDir, modelFailHint) = await EnsureModelDirectoryAsync().ConfigureAwait(false);
			if (string.IsNullOrEmpty(modelDir))
			{
				_log.LogError(
					"Vosk: modelo PT não disponível. Dica: {Hint}",
					modelFailHint ?? "(sem detalhe)");
				HintTo(viewModel, modelFailHint
					?? "Voz offline: falta o modelo PT em Resources/Raw (vosk-model-small-pt.zip ou vosk-model-small-pt-0.zip). Ver VOICE_VOSK_SETUP.txt.");
				RaiseListeningActive(false);
				return;
			}

			CancellationTokenSource cts;
			int gen;
			lock (_runGate)
			{
				cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				_cts = cts;
				_viewModel = viewModel;
				_session = new VoiceWakeSession(VoskVoiceConfig.WakePhrase);
				gen = ++_generation;
			}

			HintTo(viewModel, null);
			var token = cts.Token;
			// CancellationToken.None no Task.Run: o laço trata o cancelamento; um Task «Canceled» faria o StopAsync lançar.
			var loop = Task.Run(() => RecognitionLoopAsync(modelDir!, gen, token), CancellationToken.None);
			lock (_runGate)
			{
				if (_generation == gen)
					_loopTask = loop;
			}
			_log.LogInformation("Vosk: tarefa de reconhecimento agendada (geração {Gen}, modelo em {Path}).", gen, modelDir);
		}
		finally
		{
			_lifecycle.Release();
		}
	}

	public Task ResumeAsync()
	{
		MainViewModel? host;
		lock (_runGate)
		{
			if (_cts is not null)
				return Task.CompletedTask;
			host = _lastViewModel;
		}

		return host is null ? Task.CompletedTask : StartAsync(host);
	}

	public async Task StopAsync()
	{
		await _lifecycle.WaitAsync().ConfigureAwait(false);
		try
		{
			CancellationTokenSource? cts;
			MainViewModel? vmSnap;
			Task? loop;
			lock (_runGate)
			{
				cts = _cts;
				vmSnap = _viewModel;
				loop = _loopTask;
				_cts = null;
				_session = null;
				_viewModel = null;
				_loopTask = null;
			}
			if (vmSnap is not null)
			{
				MainThread.BeginInvokeOnMainThread(() =>
				{
					vmSnap.SetVoiceModelReadyHint(null);
					vmSnap.SetVoiceOfflineHint(null);
				});
			}
			if (cts is null)
				return;
			try
			{
				cts.Cancel();
				if (loop is not null)
					await loop.ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				_log.LogDebug(ex, "Vosk: laço terminou com erro ao parar.");
			}
			finally
			{
				cts.Dispose();
				RaiseListeningActive(false);
				_log.LogInformation("Vosk: escuta parada.");
			}
		}
		finally
		{
			_lifecycle.Release();
		}
	}

	/// <summary>
	/// O laço terminou sozinho (erro, microfone irrecuperável): limpa o estado para que um
	/// <see cref="StartAsync"/> / <see cref="ResumeAsync"/> seguinte volte a arrancar em vez de ficar «zumbi».
	/// </summary>
	void ClearStateIfCurrent(int gen)
	{
		CancellationTokenSource? orphan = null;
		lock (_runGate)
		{
			if (gen != _generation || _cts is null)
				return;
			orphan = _cts;
			_cts = null;
			_session = null;
			_viewModel = null;
			_loopTask = null;
		}
		orphan.Dispose();
		_log.LogWarning("Vosk: laço terminou sozinho (geração {Gen}) — estado limpo para permitir novo arranque.", gen);
	}

	async Task<(string? Dir, string? FailHint)> EnsureModelDirectoryAsync()
	{
		var root = Path.Combine(FileSystem.AppDataDirectory, VoskVoiceConfig.ExtractedModelDirName);
		var existing = FindModelDirectory(root);
		if (!string.IsNullOrEmpty(existing))
			return (existing, null);

		await using Stream? package = await TryOpenPackagedModelZipAsync().ConfigureAwait(false);
		if (package is null)
		{
			return (
				null,
				"Voz offline: o ZIP do modelo não está dentro do APK (inclua vosk-model-small-pt.zip ou vosk-model-small-pt-0.zip em Resources/Raw). Faça Clean, Rebuild e reinstale.");
		}

		try
		{
			Directory.CreateDirectory(root);
			var zipPath = Path.Combine(FileSystem.CacheDirectory, "vosk-model-pack.zip");
			await using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
				await package.CopyToAsync(fs).ConfigureAwait(false);
			// ZipFile.ExtractToDirectory falha com alguns ZIP Vosk (caminhos / codificação); extração manual é mais robusta.
			ExtractModelZipToDirectory(zipPath, root);
			try
			{
				File.Delete(zipPath);
			}
			catch
			{
				/* ignore */
			}
		}
		catch (Exception ex)
		{
			_log.LogError(ex, "Vosk: falha ao extrair o modelo.");
			TryDeleteExtractRoot(root);
			return (
				null,
				"Voz offline: não foi possível extrair o modelo (ZIP corrompido ou disco cheio). Volte a copiar o ZIP PT do site alphacephei.com/vosk/models.");
		}

		var found = FindModelDirectory(root);
		if (string.IsNullOrEmpty(found))
		{
			_log.LogError(
				"Vosk: após extrair, não há pasta de modelo Vosk reconhecível. Raiz: {Root}. Amostra: {Sample}",
				root,
				SummarizeExtractedTree(root));
			TryDeleteExtractRoot(root);
			return (
				null,
				"Voz offline: o ZIP não parece um modelo Vosk completo (conf/grafos ou am/*.mdl em falta). Descarregue o ZIP oficial «vosk-model-small-pt» em alphacephei.com/vosk/models (não descompacte antes de o incluir no projeto).");
		}

		return (found, null);
	}

	async Task<Stream?> TryOpenPackagedModelZipAsync()
	{
		foreach (var name in VoskVoiceConfig.PackagedModelZipCandidateNames)
		{
			try
			{
				return await FileSystem.OpenAppPackageFileAsync(name).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				if (_log.IsEnabled(LogLevel.Trace))
					_log.LogTrace(ex, "Vosk: asset «{Name}» não encontrado ou erro ao abrir.", name);
			}
		}

		return null;
	}

	static void TryDeleteExtractRoot(string root)
	{
		try
		{
			if (Directory.Exists(root))
				Directory.Delete(root, recursive: true);
		}
		catch
		{
			/* ignore */
		}
	}

	/// <summary>Extracção segura (Zip slip) com normalização de caminhos «/» dos ZIP gerados em Linux.</summary>
	static void ExtractModelZipToDirectory(string zipPath, string destinationDir)
	{
		var rootFull = Path.GetFullPath(destinationDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
		using var archive = ZipFile.OpenRead(zipPath);
		foreach (var entry in archive.Entries)
		{
			if (string.IsNullOrEmpty(entry.Name))
				continue;
			var relative = entry.FullName.Replace('\\', '/').TrimStart('/');
			if (string.IsNullOrEmpty(relative) || relative.EndsWith('/'))
				continue;
			var parts = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
			var destPath = rootFull;
			foreach (var p in parts)
				destPath = Path.Combine(destPath, p);
			destPath = Path.GetFullPath(destPath);
			if (!destPath.StartsWith(rootFull + Path.DirectorySeparatorChar, StringComparison.Ordinal)
			    && !string.Equals(destPath, rootFull, StringComparison.Ordinal))
				continue;
			var parent = Path.GetDirectoryName(destPath);
			if (!string.IsNullOrEmpty(parent))
				Directory.CreateDirectory(parent);
			entry.ExtractToFile(destPath, overwrite: true);
		}
	}

	static string SummarizeExtractedTree(string root, int maxFiles = 48)
	{
		if (!Directory.Exists(root))
			return "(directório inexistente)";
		var sb = new StringBuilder();
		var n = 0;
		try
		{
			foreach (var path in Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories))
			{
				if (n >= maxFiles)
					break;
				sb.Append(Path.GetRelativePath(root, path)).Append("; ");
				n++;
			}
		}
		catch
		{
			/* ignore */
		}

		return sb.Length == 0 ? "(vazio)" : sb.ToString();
	}

	static string? FindModelDirectory(string root)
	{
		if (!Directory.Exists(root))
			return null;
		if (DirectoryLooksLikeVoskModel(root))
			return root;
		foreach (var sub in Directory.GetDirectories(root))
		{
			var nested = FindModelDirectory(sub);
			if (!string.IsNullOrEmpty(nested))
				return nested;
		}
		return null;
	}

	/// <summary>
	/// Modelos recentes (ex. vosk-model-small-pt-0.3) usam raiz «compacta»: final.mdl, mfcc.conf, *.fst na mesma pasta.
	/// Pacotes mais antigos usam layout Kaldi: am/, conf/, graph/.
	/// </summary>
	static bool DirectoryLooksLikeVoskModel(string dir)
	{
		if (DirectoryLooksLikeVoskCompactRoot(dir))
			return true;
		return DirectoryLooksLikeVoskClassicKaldiLayout(dir);
	}

	static bool DirectoryLooksLikeVoskCompactRoot(string dir)
	{
		if (!Directory.Exists(dir))
			return false;

		var hasMfcc = File.Exists(Path.Combine(dir, "mfcc.conf"))
			|| Directory.EnumerateFiles(dir).Any(f =>
				string.Equals(Path.GetFileName(f), "mfcc.conf", StringComparison.OrdinalIgnoreCase));

		var hasFinalMdl = File.Exists(Path.Combine(dir, "final.mdl"))
			|| Directory.EnumerateFiles(dir, "*.mdl", SearchOption.TopDirectoryOnly).Any();

		var hasFstAtRoot = Directory.EnumerateFiles(dir, "*.fst", SearchOption.TopDirectoryOnly).Any();

		return hasMfcc && hasFinalMdl && hasFstAtRoot;
	}

	static bool DirectoryLooksLikeVoskClassicKaldiLayout(string dir)
	{
		var am = Path.Combine(dir, "am");
		var graphDir = Path.Combine(dir, "graph");
		var confDir = Path.Combine(dir, "conf");
		if (!Directory.Exists(am) || !Directory.Exists(graphDir) || !Directory.Exists(confDir))
			return false;

		var hasConf = Directory.Exists(confDir)
			&& Directory.EnumerateFiles(confDir).Any(f =>
			{
				var n = Path.GetFileName(f);
				return n.Equals("mfcc.conf", StringComparison.OrdinalIgnoreCase)
					|| n.Equals("model.conf", StringComparison.OrdinalIgnoreCase)
					|| n.Equals("model.yaml", StringComparison.OrdinalIgnoreCase);
			});
		if (!hasConf)
			return false;

		var hasGraph = Directory.Exists(graphDir)
			&& Directory.EnumerateFiles(graphDir, "*.fst", SearchOption.AllDirectories).Any();

		if (!hasGraph)
			return false;

		if (Directory.EnumerateFiles(am).Any(f => f.EndsWith(".mdl", StringComparison.OrdinalIgnoreCase)))
			return true;

		return Directory.EnumerateFiles(am).Any();
	}

	static string VoiceStartFailureHint(string phasePt, Exception ex)
	{
		var detail = FormatExceptionChain(ex);
		return $"Voz offline (falha em «{phasePt}»). {detail}";
	}

	static string FormatExceptionChain(Exception ex)
	{
		var parts = new List<string>();
		for (Exception? e = ex; e is not null && parts.Count < 6; e = e.InnerException)
		{
			var msg = e.Message?.Trim();
			if (string.IsNullOrEmpty(msg))
				parts.Add(e.GetType().Name);
			else
				parts.Add($"{e.GetType().Name}: {msg}");
		}

		var s = string.Join(" → ", parts);
		return s.Length <= 240 ? s : s[..237] + "…";
	}

	async Task RecognitionLoopAsync(string modelDir, int gen, CancellationToken ct)
	{
		IntPtr recognizer = IntPtr.Zero;
		MicCapture? mic = null;
		var phasePt = "início";
		try
		{
			phasePt = "carregar modelo";
			var model = GetOrCreateModel(modelDir);

			VmModelReadyHint("A preparar reconhecimento…");
			phasePt = "criar reconhecedor";
			recognizer = CreateRecognizer(model);

			var failures = 0;
			var wasSpeaking = false;
			while (!ct.IsCancellationRequested)
			{
				phasePt = "microfone";
				mic = TryOpenMic();
				if (mic is null)
				{
					failures++;
					VmHint(MicReconnectHint);
					RaiseListeningActive(false);
					await Task.Delay(ReconnectDelay(failures), ct).ConfigureAwait(false);
					continue;
				}

				VmHint(null);
				VmModelReadyHint(null);
				RaiseListeningActive(true);
				_log.LogInformation("Vosk: microfone a gravar — escuta activa.");

				var shorts = mic.Buffer;
				var lastSignalUtc = DateTime.UtcNow;
				// Maior volume (RMS por bloco) desde a última frase final — voz perto do celular chega bem mais alta.
				var utterancePeakDbfs = double.NegativeInfinity;
				string? failReason = null;
				while (!ct.IsCancellationRequested)
				{
					if (mic.Record.RecordingState != global::Android.Media.RecordState.Recording)
					{
						failReason = "AudioRecord deixou de gravar";
						break;
					}

					var nRead = mic.Record.Read(shorts, 0, shorts.Length);
					if (nRead < 0)
					{
						failReason = $"AudioRecord.Read devolveu {nRead}";
						break;
					}
					if (nRead == 0)
					{
						await Task.Delay(15, ct).ConfigureAwait(false);
						continue;
					}

					// Captura silenciada pelo Android (outra app/gravação tomou o microfone) devolve zeros exactos.
					var now = DateTime.UtcNow;
					if (HasSignal(shorts, nRead))
					{
						lastSignalUtc = now;
						failures = 0;
					}
					else if (now - lastSignalUtc > DigitalSilenceTimeout)
					{
						failReason = "só silêncio digital (captura silenciada)";
						break;
					}

					// Quando o assistente acaba de falar, descarta o áudio acumulado (eco do TTS)
					// para não se misturar com a resposta do utilizador.
					var speakingNow = CurrentViewModel()?.IsAssistantSpeaking == true;
					if (wasSpeaking && !speakingNow)
					{
						VoskJni.Reset(recognizer);
						utterancePeakDbfs = double.NegativeInfinity;
						_log.LogDebug("Vosk: reconhecedor reiniciado após fim do TTS.");
					}
					wasSpeaking = speakingNow;

					utterancePeakDbfs = Math.Max(utterancePeakDbfs, VoiceUtteranceFilter.RmsDbfs(shorts, nRead));

					var accepted = CallAcceptWaveForm(recognizer, shorts, nRead);
					if (!accepted)
						continue;

					var peakDbfs = utterancePeakDbfs;
					utterancePeakDbfs = double.NegativeInfinity;

					var json = VoskJni.GetResult(recognizer);
					var result = VoskResultParser.Parse(json);
					if (result is null)
						continue;

					var utterance = VoiceCommandGrammar.StripUnknown(result.Value.Text);
					if (utterance.Length == 0)
						continue;

					var verdict = VoiceUtteranceFilter.Evaluate(result.Value.AverageConfidence, peakDbfs);
					_log.LogInformation(
						"Vosk filtro: «{Text}» conf={Conf} nível={Level}dBFS → {Verdict}",
						utterance,
						result.Value.AverageConfidence?.ToString("0.00", CultureInfo.InvariantCulture) ?? "n/d",
						double.IsNegativeInfinity(peakDbfs) ? "-∞" : peakDbfs.ToString("0", CultureInfo.InvariantCulture),
						verdict == VoiceUtteranceVerdict.Aceite ? "aceite" : $"descartada ({VoiceUtteranceFilter.Describe(verdict)})");

					if (verdict != VoiceUtteranceVerdict.Aceite)
					{
						if (VoskVoiceConfig.ShowFilteredInBanner)
							VmLastHeard($"{utterance} (ignorado: {VoiceUtteranceFilter.Describe(verdict)})");
						continue;
					}

					VmLastHeard(utterance);
					TryDispatch(utterance);
				}

				mic.Dispose();
				mic = null;
				if (failReason is null)
					break; // cancelado (StopAsync)

				failures++;
				_log.LogWarning("Vosk: microfone perdido ({Reason}) — a reconectar (tentativa {N}).", failReason, failures);
				VmHint(MicReconnectHint);
				RaiseListeningActive(false);
				VoskJni.Reset(recognizer);
				await Task.Delay(ReconnectDelay(failures), ct).ConfigureAwait(false);
			}
		}
		catch (OperationCanceledException)
		{
			/* normal */
		}
		catch (Exception ex)
		{
			_log.LogError(ex, "Vosk: erro no laço de reconhecimento (fase: {Phase}).", phasePt);
			VmHint(VoiceStartFailureHint(phasePt, ex));
		}
		finally
		{
			RaiseListeningActive(false);
			mic?.Dispose();
			VoskJni.DeleteGlobal(recognizer);
			// O modelo fica em cache (_model) para o próximo arranque ser imediato.
			ClearStateIfCurrent(gen);
		}
	}

	IntPtr GetOrCreateModel(string modelDir)
	{
		lock (_runGate)
		{
			if (_model != IntPtr.Zero && string.Equals(_modelDir, modelDir, StringComparison.Ordinal))
				return _model;
		}

		// Carga JNI do modelo pode demorar dezenas de segundos em telemóveis modestos — só acontece uma vez.
		VmModelReadyHint("A carregar motor Vosk na memória (pode demorar na primeira vez)…");
		_log.LogInformation("Vosk: a criar org.vosk.Model em {Path}…", modelDir);
		var created = VoskJni.CreateModel(modelDir);
		_log.LogInformation("Vosk: Model JNI criado.");

		IntPtr previous;
		lock (_runGate)
		{
			previous = _model;
			_model = created;
			_modelDir = modelDir;
		}
		if (previous != IntPtr.Zero)
			VoskJni.DeleteGlobal(previous);
		return created;
	}

	static TimeSpan ReconnectDelay(int failures) =>
		TimeSpan.FromMilliseconds(Math.Min(5000, 500 * (1 << Math.Min(failures - 1, 4))));

	static bool HasSignal(short[] buffer, int length)
	{
		for (var i = 0; i < length; i++)
		{
			if (buffer[i] != 0)
				return true;
		}
		return false;
	}

	/// <summary>AudioRecord 16 kHz mono PCM16 + efeitos (eco/ruído); null se o microfone não abrir (ocupado, etc.).</summary>
	MicCapture? TryOpenMic()
	{
		const int sampleRate = 16000;
		const global::Android.Media.ChannelIn ch = global::Android.Media.ChannelIn.Mono;
		const global::Android.Media.Encoding enc = global::Android.Media.Encoding.Pcm16bit;
		global::Android.Media.AudioRecord? record = null;
		try
		{
			var minBuf = global::Android.Media.AudioRecord.GetMinBufferSize(sampleRate, ch, enc);
			if (minBuf <= 0)
			{
				_log.LogError("Vosk: GetMinBufferSize inválido ({N}).", minBuf);
				return null;
			}

			var bufferSize = Math.Max(minBuf * 2, 8192);
			record = new global::Android.Media.AudioRecord(VoskVoiceConfig.MicSource, sampleRate, ch, enc, bufferSize);
			if (record.State != global::Android.Media.State.Initialized)
			{
				_log.LogWarning("Vosk: AudioRecord não inicializado (microfone ocupado?).");
				record.Release();
				return null;
			}

			var (aec, ns) = AttachAudioEffects(record.AudioSessionId);
			record.StartRecording();
			if (record.RecordingState != global::Android.Media.RecordState.Recording)
			{
				_log.LogWarning("Vosk: StartRecording não arrancou (microfone ocupado?).");
				aec?.Release();
				ns?.Release();
				record.Release();
				return null;
			}

			return new MicCapture(record, aec, ns, new short[bufferSize / 2], _log);
		}
		catch (Exception ex)
		{
			_log.LogWarning(ex, "Vosk: falha ao abrir o microfone.");
			try
			{
				record?.Release();
			}
			catch
			{
				/* ignore */
			}
			return null;
		}
	}

	sealed class MicCapture(
		global::Android.Media.AudioRecord record,
		global::Android.Media.Audiofx.AcousticEchoCanceler? aec,
		global::Android.Media.Audiofx.NoiseSuppressor? ns,
		short[] buffer,
		ILogger log) : IDisposable
	{
		public global::Android.Media.AudioRecord Record { get; } = record;
		public short[] Buffer { get; } = buffer;

		public void Dispose()
		{
			try
			{
				aec?.Release();
				ns?.Release();
				Record.Stop();
				Record.Release();
			}
			catch (Exception ex)
			{
				log.LogDebug(ex, "Vosk: libertar AudioRecord.");
			}
		}
	}

	MainViewModel? CurrentViewModel()
	{
		lock (_runGate)
			return _viewModel;
	}

	/// <summary>Sem isto o resultado não traz «conf» por palavra e o filtro de confiança fica desligado.</summary>
	void EnableWordConfidence(IntPtr recognizer)
	{
		try
		{
			VoskJni.SetWords(recognizer, true);
		}
		catch (Exception ex)
		{
			_log.LogWarning(ex, "Vosk: setWords falhou — filtro de confiança desactivado.");
		}
	}

	/// <summary>Com gramática de comandos (mais preciso); se falhar, volta ao texto livre.</summary>
	IntPtr CreateRecognizer(IntPtr model)
	{
		if (VoskVoiceConfig.UseCommandGrammar)
		{
			try
			{
				var extra = CurrentViewModel()?.GetVoiceCommandEntries().Select(e => e.OriginalTitle)
					?? Enumerable.Empty<string>();
				var grammar = VoiceCommandGrammar.BuildJson(extra);
				var r = VoskJni.CreateRecognizer(model, 16000f, grammar);
				EnableWordConfidence(r);
				_log.LogInformation("Vosk: Recognizer JNI criado com gramática de comandos: {Grammar}", grammar);
				return r;
			}
			catch (Exception ex)
			{
				_log.LogWarning(ex, "Vosk: gramática de comandos falhou — a usar texto livre.");
			}
		}

		var free = VoskJni.CreateRecognizer(model, 16000f);
		EnableWordConfidence(free);
		_log.LogInformation("Vosk: Recognizer JNI criado (texto livre).");
		return free;
	}

	/// <summary>Cancelamento de eco e supressão de ruído do próprio aparelho, quando existem.</summary>
	(global::Android.Media.Audiofx.AcousticEchoCanceler?, global::Android.Media.Audiofx.NoiseSuppressor?) AttachAudioEffects(int audioSessionId)
	{
		global::Android.Media.Audiofx.AcousticEchoCanceler? aec = null;
		global::Android.Media.Audiofx.NoiseSuppressor? ns = null;
		try
		{
			if (global::Android.Media.Audiofx.AcousticEchoCanceler.IsAvailable)
			{
				aec = global::Android.Media.Audiofx.AcousticEchoCanceler.Create(audioSessionId);
				aec?.SetEnabled(true);
			}
			if (global::Android.Media.Audiofx.NoiseSuppressor.IsAvailable)
			{
				ns = global::Android.Media.Audiofx.NoiseSuppressor.Create(audioSessionId);
				ns?.SetEnabled(true);
			}
			_log.LogInformation("Vosk: cancelamento de eco {Aec}, supressão de ruído {Ns}.",
				aec is null ? "indisponível" : "activo",
				ns is null ? "indisponível" : "activa");
		}
		catch (Exception ex)
		{
			_log.LogDebug(ex, "Vosk: falha ao ligar efeitos de áudio.");
		}
		return (aec, ns);
	}

	static bool CallAcceptWaveForm(IntPtr recognizer, short[] buffer, int length)
	{
		var slice = new short[length];
		Array.Copy(buffer, 0, slice, 0, length);
		return VoskJni.AcceptWaveForm(recognizer, slice);
	}

	void TryDispatch(string utterance)
	{
		MainViewModel? vm;
		VoiceWakeSession? session;
		lock (_runGate)
		{
			vm = _viewModel;
			session = _session;
		}
		if (vm is null || session is null)
			return;

		// Barge-in: «pare» / similares interrompe o TTS sem wake.
		if (vm.TryHandleStopSpeechUtterance(utterance))
			return;

		// Enquanto o assistente fala, ignora outros comandos (reduz eco do TTS).
		if (vm.IsAssistantSpeaking)
		{
			_log.LogDebug("Vosk: ignorada durante a fala: «{Text}»", utterance);
			return;
		}

		// Frase fechada logo depois do fim da fala — quase sempre eco do TTS.
		if (DateTime.UtcNow - vm.AssistantSpeechEndedUtc < VoskVoiceConfig.IgnoreAfterSpeechEnd)
		{
			_log.LogDebug("Vosk: ignorada (eco pós-TTS): «{Text}»", utterance);
			return;
		}

		// Confirmação sim/não de protocolo: aceita a frase sem wake «olá sistema».
		if (vm.TryHandleProtocolYesNoUtterance(utterance))
		{
			_log.LogDebug("Vosk: entregue à pergunta sim/não: «{Text}»", utterance);
			return;
		}

		var entries = vm.GetVoiceCommandEntries();
		var kind = session.TryEvaluate(utterance, entries, out var menuTitle);
		_log.LogDebug("Vosk: «{Text}» → {Kind} {Title}", utterance, kind, menuTitle);
		switch (kind)
		{
			case VoiceWakeSession.DispatchKind.MenuCommand:
				if (MainThread.IsMainThread)
					vm.TryInvokeMenuItemFromVoiceCaption(menuTitle!);
				else
					MainThread.BeginInvokeOnMainThread(() => vm.TryInvokeMenuItemFromVoiceCaption(menuTitle!));
				break;
			case VoiceWakeSession.DispatchKind.WakeWithoutMenuCommand:
				if (MainThread.IsMainThread)
					vm.NotifyVoiceWakeWithoutCommandDebounced();
				else
					MainThread.BeginInvokeOnMainThread(vm.NotifyVoiceWakeWithoutCommandDebounced);
				break;
			case VoiceWakeSession.DispatchKind.Nothing:
			default:
				break;
		}
	}
}
