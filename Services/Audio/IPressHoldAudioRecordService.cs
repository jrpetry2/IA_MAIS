namespace SaphMovel.Services.Audio;

/// <summary>Grava áudio enquanto o botão «ouvir» está pressionado.</summary>
public interface IPressHoldAudioRecordService
{
	bool IsRecording { get; }

	/// <summary>Caminho do ficheiro de saída (sempre sobrescrito em novo arranque).</summary>
	string OutputFilePath { get; }

	Task<bool> StartAsync();

	Task StopAsync();

	/// <summary>Pico de amplitude (0–32767) desde a última leitura; 0 se não está a gravar. Serve para detectar silêncio.</summary>
	int ReadMaxAmplitude();
}
