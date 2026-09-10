using Android.Media;

namespace SaphMovel.Platforms.Android.Audio;

/// <summary>Gravação de voz via <see cref="MediaRecorder"/> (AAC/MP4 — qualidade estável no Android).</summary>
sealed class MediaRecorderVoiceCapture : IDisposable
{
	const int SampleRateHz = 16_000;
	const int BitRateBps = 128_000;

	MediaRecorder? _recorder;
	string? _outputPath;

	public bool IsRecording { get; private set; }

	public bool Start(string outputPath)
	{
		StopAndRelease();

		var dir = Path.GetDirectoryName(outputPath);
		if (!string.IsNullOrEmpty(dir))
			Directory.CreateDirectory(dir);
		if (File.Exists(outputPath))
			File.Delete(outputPath);

		_outputPath = outputPath;
		_recorder = new MediaRecorder();
		_recorder.SetAudioSource(AudioSource.VoiceRecognition);
		_recorder.SetOutputFormat(OutputFormat.Mpeg4);
		_recorder.SetAudioEncoder(AudioEncoder.Aac);
		_recorder.SetAudioSamplingRate(SampleRateHz);
		_recorder.SetAudioChannels(1);
		_recorder.SetAudioEncodingBitRate(BitRateBps);
		_recorder.SetOutputFile(outputPath);
		_recorder.Prepare();
		_recorder.Start();
		IsRecording = true;
		return true;
	}

	public bool Stop()
	{
		if (!IsRecording || _recorder is null)
			return false;

		IsRecording = false;
		try
		{
			_recorder.Stop();
		}
		catch (Java.Lang.IllegalStateException)
		{
			// Gravação demasiado curta (< ~1 s) — ficheiro pode ficar inválido.
		}
		catch (Exception)
		{
			/* ignore */
		}
		finally
		{
			StopAndRelease();
		}

		return !string.IsNullOrEmpty(_outputPath)
			&& File.Exists(_outputPath)
			&& new FileInfo(_outputPath).Length > 0;
	}

	void StopAndRelease()
	{
		IsRecording = false;
		try
		{
			_recorder?.Reset();
		}
		catch
		{
			/* ignore */
		}

		try
		{
			_recorder?.Release();
		}
		catch
		{
			/* ignore */
		}

		try
		{
			_recorder?.Dispose();
		}
		catch
		{
			/* ignore */
		}

		_recorder = null;
	}

	public void Dispose() => StopAndRelease();
}
