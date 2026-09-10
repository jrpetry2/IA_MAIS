using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using SaphMovel.Services.Audio;
using SaphMovel.Services.Voice;

namespace SaphMovel.Platforms.Android.Audio;

/// <summary>
/// Gravação «pressionar para falar»: <see cref="MediaRecorder"/> AAC/MP4 em <see cref="SinaisVitaisAudioPaths.AppRootRecordingFile"/>.
/// </summary>
public sealed class SinaisVitaisPressRecordService : IPressHoldAudioRecordService
{
	readonly ILogger<SinaisVitaisPressRecordService> _log;
	readonly IVoskListeningService? _vosk;
	readonly object _gate = new();

	MediaRecorderVoiceCapture? _capture;
	bool _recording;
	bool _voskWasStoppedForRecording;

	public SinaisVitaisPressRecordService(
		ILogger<SinaisVitaisPressRecordService> log,
		IVoskListeningService? vosk = null)
	{
		_log = log;
		_vosk = vosk;
	}

	public bool IsRecording
	{
		get
		{
			lock (_gate)
				return _recording;
		}
	}

	public string OutputFilePath => SinaisVitaisAudioPaths.AppRootRecordingFile;

	public async Task<bool> StartAsync()
	{
		lock (_gate)
		{
			if (_recording)
				return true;
		}

		var status = MainThread.IsMainThread
			? await Permissions.RequestAsync<Permissions.Microphone>().ConfigureAwait(false)
			: await MainThread.InvokeOnMainThreadAsync(Permissions.RequestAsync<Permissions.Microphone>)
				.ConfigureAwait(false);
		if (status != PermissionStatus.Granted)
		{
			_log.LogWarning("Gravação Sinais Vitais: permissão de microfone negada.");
			return false;
		}

		if (_vosk is not null)
		{
			_voskWasStoppedForRecording = true;
			try
			{
				await _vosk.StopAsync().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				_voskWasStoppedForRecording = false;
				_log.LogDebug(ex, "Gravação: falha ao parar Vosk antes de gravar.");
			}

			await Task.Delay(300).ConfigureAwait(false);
		}

		return await MainThread.InvokeOnMainThreadAsync(StartCaptureOnUiThread).ConfigureAwait(false);
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
		await MainThread.InvokeOnMainThreadAsync(StopCaptureOnUiThread).ConfigureAwait(false);

		if (!_voskWasStoppedForRecording || _vosk is null)
			return;

		// Vosk só reactiva quando o utilizador já não está a gravar (evita disputa no microfone).
		_voskWasStoppedForRecording = false;
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
