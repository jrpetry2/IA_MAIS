using Microsoft.Extensions.Logging;
using Microsoft.Maui.Media;
using SaphEventos;
using SaphMovel.Models;
using SaphMovel.ViewModels;

namespace SaphMovel.Services.Voice;

public sealed class ChamadoSpeechService : IChamadoSpeechService
{
	readonly ILogger<ChamadoSpeechService> _log;
	readonly IVoskListeningService? _vosk;
	readonly object _gate = new();
	CancellationTokenSource? _cts;
	volatile bool _isSpeaking;
	long _lastSpeechEndedTicks = DateTime.MinValue.Ticks;

	public ChamadoSpeechService(ILogger<ChamadoSpeechService> log, IVoskListeningService? vosk = null)
	{
		_log = log;
		_vosk = vosk;
	}

	public bool IsSpeaking => _isSpeaking;

	public DateTime LastSpeechEndedUtc => new(Interlocked.Read(ref _lastSpeechEndedTicks), DateTimeKind.Utc);

	/// <summary>Só a fala mais recente (dona do <see cref="_cts"/>) limpa o estado — uma fala cancelada não pode marcar «parou de falar».</summary>
	void EndSpeech(CancellationTokenSource linked)
	{
		lock (_gate)
		{
			if (!ReferenceEquals(_cts, linked))
				return;
			_isSpeaking = false;
			Interlocked.Exchange(ref _lastSpeechEndedTicks, DateTime.UtcNow.Ticks);
			_cts.Dispose();
			_cts = null;
		}
	}

	public void Cancel()
	{
		lock (_gate)
		{
			try
			{
				_cts?.Cancel();
			}
			catch (ObjectDisposedException)
			{
				/* ignore */
			}
		}
	}

	public Task SpeakChamadoAsync(
		ChamadoPayload payload,
		MainViewModel? voskHost = null,
		CancellationToken cancellationToken = default)
	{
		var script = ChamadoSpeechScriptBuilder.Build(payload);
		_log.LogInformation("TTS Ciente: {Script}", script);
		return SpeakScriptAsync(script, voskHost, cancellationToken);
	}

	public Task SpeakPrescricaoAsync(
		PrescricaoPacote pacote,
		MainViewModel? voskHost = null,
		string? nomeEquipe = null,
		CancellationToken cancellationToken = default)
	{
		var script = PrescricaoSpeechScriptBuilder.Build(pacote, nomeEquipe);
		_log.LogInformation("TTS Prescrição: {Script}", script);
		return SpeakScriptAsync(script, voskHost, cancellationToken);
	}

	public Task SpeakTextAsync(
		string text,
		MainViewModel? voskHost = null,
		CancellationToken cancellationToken = default)
	{
		var script = string.IsNullOrWhiteSpace(text) ? "" : text.Trim();
		if (script.Length == 0)
			return Task.CompletedTask;
		_log.LogInformation("TTS texto: {Script}", script);
		return SpeakScriptAsync(script, voskHost, cancellationToken);
	}

	async Task SpeakScriptAsync(string script, MainViewModel? voskHost, CancellationToken cancellationToken)
	{
		Cancel();
		CancellationTokenSource linked;
		lock (_gate)
		{
			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			linked = _cts;
		}

		var token = linked.Token;
		// Mantém Vosk activo durante a fala para barge-in («pare»). Não pausar o microfone.
		if (voskHost is not null && _vosk is not null && voskHost.IsVoiceMicrophoneActive)
		{
			try
			{
				await _vosk.StartAsync(voskHost).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				_log.LogDebug(ex, "TTS: falha ao garantir Vosk durante fala.");
			}
		}

		_isSpeaking = true;
		try
		{
			var options = await BuildSpeechOptionsAsync(preferred: null, token).ConfigureAwait(false);
			var speakTask = TextToSpeech.Default.SpeakAsync(script, options, token);

			// Watchdog: em alguns aparelhos o SpeakAsync nunca termina (outra app tomou o áudio) e
			// IsSpeaking ficava preso — o Vosk ignorava todos os comandos excepto «pare».
			using var watchdogCts = new CancellationTokenSource();
			var limit = MaxSpeechDuration(script);
			var finished = await Task.WhenAny(speakTask, Task.Delay(limit, watchdogCts.Token)).ConfigureAwait(false);
			if (finished == speakTask)
			{
				watchdogCts.Cancel();
				await speakTask.ConfigureAwait(false);
			}
			else
			{
				_log.LogWarning("TTS: fala não terminou em {Limit} — cancelada pelo watchdog.", limit);
				_ = speakTask.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
				try
				{
					linked.Cancel();
				}
				catch (ObjectDisposedException)
				{
					/* ignore */
				}
			}
		}
		catch (OperationCanceledException)
		{
			_log.LogDebug("TTS: cancelado.");
		}
		catch (Exception ex)
		{
			_log.LogWarning(ex, "TTS: falha ao falar. Verifique se o pacote de voz PT-BR está instalado no dispositivo.");
		}
		finally
		{
			EndSpeech(linked);
		}
	}

	/// <summary>Duração máxima plausível de uma fala (~90 ms por carácter + folga de arranque do motor TTS).</summary>
	static TimeSpan MaxSpeechDuration(string script) =>
		TimeSpan.FromMilliseconds(script.Length * 90 + 8000);

	public async Task<IReadOnlyList<TtsVoiceOption>> GetAvailableVoicesAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			var locales = await TextToSpeech.Default.GetLocalesAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
			return locales
				.Select(l => new TtsVoiceOption
				{
					Id = l.Id ?? "",
					Language = l.Language ?? "",
					Country = l.Country ?? "",
					Name = string.IsNullOrWhiteSpace(l.Name) ? $"{l.Language}-{l.Country}" : l.Name!,
				})
				.OrderByDescending(v => IsPortuguese(v))
				.ThenBy(v => v.DisplayTitle, StringComparer.OrdinalIgnoreCase)
				.ToList();
		}
		catch (Exception ex)
		{
			_log.LogWarning(ex, "TTS: não foi possível listar vozes.");
			return Array.Empty<TtsVoiceOption>();
		}
	}

	public TtsVoiceOption? GetSelectedVoiceOrNull(IReadOnlyList<TtsVoiceOption> available) =>
		FindSavedVoice(available);

	public void SelectVoice(TtsVoiceOption? voice)
	{
		if (voice is null)
		{
			TtsVoicePreferences.Clear();
			_log.LogInformation("TTS: voz automática (preferência limpa).");
			return;
		}

		TtsVoicePreferences.Save(voice.Id, voice.Language, voice.Country, voice.Name);
		_log.LogInformation("TTS: voz seleccionada «{Name}» ({Id})", voice.Name, voice.Id);
	}

	public async Task SpeakPreviewAsync(
		string text,
		TtsVoiceOption? voice = null,
		CancellationToken cancellationToken = default)
	{
		Cancel();
		CancellationTokenSource linked;
		lock (_gate)
		{
			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			linked = _cts;
		}

		var token = linked.Token;
		_isSpeaking = true;
		try
		{
			var options = await BuildSpeechOptionsAsync(voice, token).ConfigureAwait(false);
			await TextToSpeech.Default.SpeakAsync(
				string.IsNullOrWhiteSpace(text) ? "Olá equipe, teste de voz do SAPH Móvel." : text.Trim(),
				options,
				token).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			/* ignore */
		}
		catch (Exception ex)
		{
			_log.LogWarning(ex, "TTS: falha na pré-visualização.");
		}
		finally
		{
			EndSpeech(linked);
		}
	}

	async Task<SpeechOptions> BuildSpeechOptionsAsync(TtsVoiceOption? preferred, CancellationToken cancellationToken)
	{
		Locale? locale = null;
		try
		{
			var locales = (await TextToSpeech.Default.GetLocalesAsync().WaitAsync(cancellationToken).ConfigureAwait(false))
				.ToList();

			if (preferred is not null)
				locale = MatchLocale(locales, preferred.Id, preferred.Language, preferred.Country, preferred.Name);

			locale ??= FindSavedLocale(locales);
			locale ??= PickDefaultPortuguese(locales);
		}
		catch
		{
			/* usa default do SO */
		}

		return new SpeechOptions
		{
			Pitch = 1.0f,
			Volume = 1.0f,
			Locale = locale,
		};
	}

	static Locale? FindSavedLocale(IEnumerable<Locale> locales)
	{
		if (!TtsVoicePreferences.HasSelection)
			return null;
		var (id, lang, country, name) = TtsVoicePreferences.Load();
		return MatchLocale(locales, id, lang, country, name);
	}

	TtsVoiceOption? FindSavedVoice(IReadOnlyList<TtsVoiceOption> available)
	{
		if (!TtsVoicePreferences.HasSelection || available.Count == 0)
			return null;
		var (id, lang, country, name) = TtsVoicePreferences.Load();
		if (!string.IsNullOrWhiteSpace(id))
		{
			var byId = available.FirstOrDefault(v => string.Equals(v.Id, id, StringComparison.OrdinalIgnoreCase));
			if (byId is not null)
				return byId;
		}

		return available.FirstOrDefault(v =>
			string.Equals(v.Language, lang, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(v.Country ?? "", country ?? "", StringComparison.OrdinalIgnoreCase)
			&& (string.IsNullOrWhiteSpace(name)
			    || string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase)));
	}

	static Locale? MatchLocale(
		IEnumerable<Locale> locales,
		string? id,
		string? language,
		string? country,
		string? name)
	{
		var list = locales as IList<Locale> ?? locales.ToList();
		if (!string.IsNullOrWhiteSpace(id))
		{
			var byId = list.FirstOrDefault(l => string.Equals(l.Id, id, StringComparison.OrdinalIgnoreCase));
			if (byId is not null)
				return byId;
		}

		return list.FirstOrDefault(l =>
			string.Equals(l.Language, language, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(l.Country ?? "", country ?? "", StringComparison.OrdinalIgnoreCase)
			&& (string.IsNullOrWhiteSpace(name)
			    || string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase)));
	}

	static Locale? PickDefaultPortuguese(IEnumerable<Locale> locales)
	{
		var list = locales as IList<Locale> ?? locales.ToList();
		return list.FirstOrDefault(l =>
			       string.Equals(l.Language, "pt-BR", StringComparison.OrdinalIgnoreCase)
			       || string.Equals(l.Language, "pt_BR", StringComparison.OrdinalIgnoreCase)
			       || (l.Language?.StartsWith("pt", StringComparison.OrdinalIgnoreCase) == true
			           && (l.Country?.Equals("BR", StringComparison.OrdinalIgnoreCase) == true
			               || string.IsNullOrEmpty(l.Country))))
		       ?? list.FirstOrDefault(l => l.Language?.StartsWith("pt", StringComparison.OrdinalIgnoreCase) == true);
	}

	static bool IsPortuguese(TtsVoiceOption v) =>
		v.Language.StartsWith("pt", StringComparison.OrdinalIgnoreCase)
		|| string.Equals(v.Country, "BR", StringComparison.OrdinalIgnoreCase);

	static TtsVoiceOption ToOption(Locale l) => new()
	{
		Id = l.Id ?? "",
		Language = l.Language ?? "",
		Country = l.Country ?? "",
		Name = string.IsNullOrWhiteSpace(l.Name) ? $"{l.Language}-{l.Country}" : l.Name!,
	};
}
