using System.Collections.Generic;
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

	CancellationTokenSource? _cts;
	Task? _loopTask;
	VoiceWakeSession? _session;
	MainViewModel? _viewModel;

	public event EventHandler<bool>? ListeningActiveChanged;

	public VoskListeningService(ILogger<VoskListeningService> log) => _log = log;

	void RaiseListeningActive(bool active)
	{
		MainThread.BeginInvokeOnMainThread(() =>
			ListeningActiveChanged?.Invoke(this, active));
	}

	void VmHint(string? message)
	{
		if (_viewModel is null)
			return;
		MainThread.BeginInvokeOnMainThread(() =>
			_viewModel.SetVoiceOfflineHint(message));
	}

	void VmModelReadyHint(string? message)
	{
		if (_viewModel is null)
			return;
		MainThread.BeginInvokeOnMainThread(() =>
			_viewModel.SetVoiceModelReadyHint(message));
	}

	public async Task StartAsync(MainViewModel viewModel, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(viewModel);
		lock (_runGate)
		{
			if (_cts is not null)
				return;
			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			_viewModel = viewModel;
			_session = new VoiceWakeSession(VoskVoiceConfig.WakePhrase);
		}

		// Android só mostra o diálogo de permissão quando o pedido é feito na thread principal (Activity).
		var status = await MainThread.InvokeOnMainThreadAsync(() =>
			Permissions.RequestAsync<Permissions.Microphone>());
		if (status != PermissionStatus.Granted)
		{
			_log.LogWarning("Vosk: permissão de microfone negada.");
			VmHint("Voz offline: conceda permissão de microfone nas definições da app para ser ouvido.");
			lock (_runGate)
			{
				_cts?.Dispose();
				_cts = null;
				_viewModel = null;
				_session = null;
			}
			RaiseListeningActive(false);
			return;
		}

		var (modelDir, modelFailHint) = await EnsureModelDirectoryAsync().ConfigureAwait(true);
		if (string.IsNullOrEmpty(modelDir))
		{
			_log.LogError(
				"Vosk: modelo PT não disponível. Dica: {Hint}",
				modelFailHint ?? "(sem detalhe)");
			VmHint(modelFailHint
				?? "Voz offline: falta o modelo PT em Resources/Raw (vosk-model-small-pt.zip ou vosk-model-small-pt-0.zip). Ver VOICE_VOSK_SETUP.txt.");
			lock (_runGate)
			{
				_cts?.Dispose();
				_cts = null;
				_viewModel = null;
				_session = null;
			}
			RaiseListeningActive(false);
			return;
		}

		VmHint(null);
		VmModelReadyHint("Modelo PT pronto — a carregar motor Vosk…");
		var token = _cts!.Token;
		_loopTask = Task.Run(() => RecognitionLoopAsync(modelDir!, token), token);
		_log.LogInformation("Vosk: tarefa de reconhecimento agendada (modelo em {Path}).", modelDir);
	}

	public async Task StopAsync()
	{
		CancellationTokenSource? cts;
		MainViewModel? vmSnap;
		lock (_runGate)
		{
			cts = _cts;
			vmSnap = _viewModel;
			_cts = null;
			_session = null;
			_viewModel = null;
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
			if (_loopTask is not null)
				await _loopTask.ConfigureAwait(false);
		}
		finally
		{
			cts.Dispose();
			_loopTask = null;
			RaiseListeningActive(false);
			_log.LogInformation("Vosk: escuta parada.");
		}
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

	async Task RecognitionLoopAsync(string modelDir, CancellationToken ct)
	{
		IntPtr model = IntPtr.Zero;
		IntPtr recognizer = IntPtr.Zero;
		global::Android.Media.AudioRecord? mic = null;
		var phasePt = "início";
		try
		{
			// Carga JNI do modelo pode demorar dezenas de segundos em telemóveis modestos — não é o microfone ainda.
			VmModelReadyHint("A carregar motor Vosk na memória (pode demorar na primeira vez)…");
			phasePt = "carregar modelo";
			_log.LogInformation("Vosk: a criar org.vosk.Model em {Path}…", modelDir);
			model = VoskJni.CreateModel(modelDir);
			_log.LogInformation("Vosk: Model JNI criado.");

			VmModelReadyHint("A preparar reconhecimento…");
			phasePt = "criar reconhecedor";
			recognizer = VoskJni.CreateRecognizer(model, 16000f);
			_log.LogInformation("Vosk: Recognizer JNI criado.");

			VmModelReadyHint("A abrir o microfone…");
			phasePt = "microfone";

			const int sampleRate = 16000;
			const global::Android.Media.ChannelIn ch = global::Android.Media.ChannelIn.Mono;
			const global::Android.Media.Encoding enc = global::Android.Media.Encoding.Pcm16bit;
			var minBuf = global::Android.Media.AudioRecord.GetMinBufferSize(sampleRate, ch, enc);
			if (minBuf <= 0)
			{
				_log.LogError("Vosk: GetMinBufferSize inválido ({N}).", minBuf);
				VmHint("Voz offline: erro ao preparar o microfone (buffer áudio).");
				RaiseListeningActive(false);
				return;
			}

			var bufferSize = Math.Max(minBuf * 2, 8192);
			mic = new global::Android.Media.AudioRecord(global::Android.Media.AudioSource.VoiceRecognition, sampleRate, ch, enc, bufferSize);
			if (mic.State != global::Android.Media.State.Initialized)
			{
				_log.LogError("Vosk: AudioRecord não inicializado.");
				VmHint("Voz offline: não foi possível abrir o microfone.");
				RaiseListeningActive(false);
				return;
			}

			var shorts = new short[bufferSize / 2];
			mic.StartRecording();
			VmHint(null);
			VmModelReadyHint(null);
			RaiseListeningActive(true);
			_log.LogInformation("Vosk: microfone a gravar — escuta activa.");
			while (!ct.IsCancellationRequested)
			{
				var nRead = mic.Read(shorts, 0, shorts.Length);
				if (nRead <= 0)
				{
					await Task.Delay(15, ct).ConfigureAwait(false);
					continue;
				}

				var accepted = CallAcceptWaveForm(recognizer, shorts, nRead);
				if (!accepted)
					continue;

				var json = VoskJni.GetResult(recognizer);
				var text = ExtractJsonText(json);
				if (string.IsNullOrWhiteSpace(text))
					continue;

				var utterance = text.Trim();
				if (_log.IsEnabled(LogLevel.Debug))
					_log.LogDebug("Vosk frase final: «{Text}»", utterance);

				// Uma única hipótese por iteração — evita juntar várias frases finais e misturar
				// wake com comandos anteriores (a fila + debounce antigo acumulava lixo durante 2,5s).
				if (_log.IsEnabled(LogLevel.Debug))
					_log.LogDebug("Vosk a avaliar: «{Utterance}»", utterance);
				TryDispatch(utterance);

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
			try
			{
				mic?.Stop();
				mic?.Release();
			}
			catch (Exception ex)
			{
				_log.LogDebug(ex, "Vosk: libertar AudioRecord.");
			}
			VoskJni.DeleteGlobal(recognizer);
			VoskJni.DeleteGlobal(model);
		}
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

		var entries = vm.MenuItems
			.Select(m => (m.Title, VoiceWakeSession.Normalize(m.Title)))
			.ToArray();
		var kind = session.TryEvaluate(utterance, entries, out var menuTitle);
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

	static string? ExtractJsonText(string? json)
	{
		if (string.IsNullOrWhiteSpace(json))
			return null;
		try
		{
			using var doc = JsonDocument.Parse(json);
			if (doc.RootElement.TryGetProperty("text", out var t))
				return t.GetString();
			if (doc.RootElement.TryGetProperty("partial", out var p))
				return p.GetString();
		}
		catch
		{
			/* ignore */
		}
		return null;
	}
}
