using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using SaphMovel.Services.Audio;
using SaphMovel.Services.Voice;

namespace SaphMovel.Platforms.Android.Audio;

/// <summary>
/// Gravação «pressionar para falar»: MediaRecorder AAC/MP4 em <see cref="SinaisVitaisAudioPaths.AppRootRecordingFile"/>.
/// </summary>
public sealed class SinaisVitaisPressRecordService : IPressHoldAudioRecordService
{
	readonly ILogger<SinaisVitaisPressRecordService> _log;
	readonly IVoskListeningService? _vosk;
	readonly object _gate = new();

	MediaRecorderVoiceCapture? _capture;
	bool _recording;
	bool _voskWasStoppedForRecording;

	/// <summary>Arranque em curso (permissão, parar Vosk, 300 ms) — o botão pode ser solto antes de acabar.</summary>
	TaskCompletionSource<bool>? _starting;

	/// <summary>O utilizador soltou o botão durante o arranque: não começar (ou parar logo) a gravação.</summary>
	bool _stopRequested;

	public SinaisVitaisPressRecordService(
		ILogger<SinaisVitaisPressRecordService> log,
		IVoskListeningService? vosk = null)
	{
		_log = log;
		_vosk = vosk;
	}

	/// <summary>Inclui o arranque em curso, para que soltar o botão cedo ainda chame <see cref="StopAsync"/>.</summary>
	public bool IsRecording
	{
		get
		{
			lock (_gate)
				return _recording || _starting is not null;
		}
	}

	public string OutputFilePath => SinaisVitaisAudioPaths.AppRootRecordingFile;

	public async Task<bool> StartAsync()
	{
		var mine = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		TaskCompletionSource<bool>? existing;
		lock (_gate)
		{
			if (_recording)
				return true;
			existing = _starting;
			if (existing is null)
			{
				_starting = mine;
				_stopRequested = false;
			}
		}

		if (existing is not null)
			return await existing.Task.ConfigureAwait(false);

		var ok = false;
		try
		{
			ok = await StartCoreAsync().ConfigureAwait(false);
		}
		finally
		{
			lock (_gate)
				_starting = null;
			mine.TrySetResult(ok);
		}
		return ok;
	}

	async Task<bool> StartCoreAsync()
	{
		// Sem isto, soltar o botão cedo transcrevia a gravação anterior (outro paciente / outro ecrã).
		TryDeleteOutputFile();

		var status = MainThread.IsMainThread
			? await Permissions.RequestAsync<Permissions.Microphone>().ConfigureAwait(false)
			: await MainThread.InvokeOnMainThreadAsync(Permissions.RequestAsync<Permissions.Microphone>)
				.ConfigureAwait(false);
		if (status != PermissionStatus.Granted)
		{
			_log.LogWarning("Gravação Sinais Vitais: permissão de microfone negada.");
			return false;
		}

		// Só pára (e depois retoma) o Vosk se ele estava mesmo a ouvir.
		if (_vosk is not null && _vosk.IsListening)
		{
			try
			{
				await _vosk.StopAsync().ConfigureAwait(false);
				lock (_gate)
					_voskWasStoppedForRecording = true;
			}
			catch (Exception ex)
			{
				_log.LogDebug(ex, "Gravação: falha ao parar Vosk antes de gravar.");
			}

			await Task.Delay(300).ConfigureAwait(false);
		}

		bool stopRequested;
		lock (_gate)
			stopRequested = _stopRequested;
		if (stopRequested)
		{
			_log.LogInformation("Gravação: botão solto antes de a gravação começar — cancelada.");
			await ResumeVoskIfStoppedAsync().ConfigureAwait(false);
			return false;
		}

		var ok = await MainThread.InvokeOnMainThreadAsync(StartCaptureOnUiThread).ConfigureAwait(false);
		if (!ok)
			await ResumeVoskIfStoppedAsync().ConfigureAwait(false);
		return ok;
	}

	void TryDeleteOutputFile()
	{
		try
		{
			if (File.Exists(OutputFilePath))
				File.Delete(OutputFilePath);
		}
		catch (Exception ex)
		{
			_log.LogDebug(ex, "Gravação: não foi possível apagar a gravação anterior.");
		}
	}

	bool StartCaptureOnUiThread()
	{
		lock (_gate)
		{
			if (_recording)
				return true;

			try
			{
				_capture?.Dispose();
				_capture = new MediaRecorderVoiceCapture();
				if (!_capture.Start(OutputFilePath))
				{
					_capture.Dispose();
					_capture = null;
					return false;
				}

				_recording = true;
				_log.LogInformation("Gravação AAC iniciada → {Path}", OutputFilePath);
				return true;
			}
			catch (Exception ex)
			{
				_log.LogError(ex, "Gravação Sinais Vitais: falha ao iniciar MediaRecorder.");
				ReleaseCaptureLocked();
				return false;
			}
		}
	}

	public async Task StopAsync()
	{
		TaskCompletionSource<bool>? starting;
		lock (_gate)
		{
			starting = _starting;
			if (starting is not null)
				_stopRequested = true;
		}

		// Botão solto durante o arranque: espera que termine para não deixar o MediaRecorder a gravar sozinho.
		if (starting is not null)
			await starting.Task.ConfigureAwait(false);

		await MainThread.InvokeOnMainThreadAsync(StopCaptureOnUiThread).ConfigureAwait(false);
		await ResumeVoskIfStoppedAsync().ConfigureAwait(false);
	}

	public int ReadMaxAmplitude()
	{
		lock (_gate)
			return _recording ? _capture?.ReadMaxAmplitude() ?? 0 : 0;
	}

	/// <summary>Vosk só reactiva quando o utilizador já não está a gravar (evita disputa no microfone).</summary>
	async Task ResumeVoskIfStoppedAsync()
	{
		bool resume;
		lock (_gate)
		{
			resume = _voskWasStoppedForRecording;
			_voskWasStoppedForRecording = false;
		}
		if (!resume || _vosk is null)
			return;

		try
		{
			await _vosk.ResumeAsync().ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_log.LogDebug(ex, "Gravação: falha ao retomar Vosk depois de gravar.");
		}
	}

	void StopCaptureOnUiThread()
	{
		lock (_gate)
		{
			if (!_recording)
				return;

			try
			{
				var ok = _capture?.Stop() == true;
				if (!ok)
					_log.LogWarning("Gravação AAC: ficheiro inválido ou vazio em {Path}", OutputFilePath);
				else
					_log.LogInformation("Gravação AAC concluída → {Path}", OutputFilePath);
			}
			catch (Exception ex)
			{
				_log.LogError(ex, "Gravação Sinais Vitais: falha ao parar MediaRecorder.");
			}
			finally
			{
				ReleaseCaptureLocked();
			}
		}
	}

	void ReleaseCaptureLocked()
	{
		_recording = false;
		_capture?.Dispose();
		_capture = null;
	}
}
