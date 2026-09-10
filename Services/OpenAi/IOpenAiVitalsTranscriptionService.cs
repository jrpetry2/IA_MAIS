namespace SaphMovel.Services.OpenAi;

/// <summary>Áudio → transcrição OpenAI → extração clínica (chat) → JSON de sinais vitais.</summary>
public interface IOpenAiVitalsTranscriptionService
{
	/// <summary>Transcreve o áudio, envia o texto ao extrator clínico e devolve JSON para o mapper.</summary>
	Task<OpenAiVitalsTranscriptionResult> TranscribeVitalsJsonAsync(
		string audioFilePath,
		CancellationToken cancellationToken = default);
}

public readonly record struct OpenAiVitalsTranscriptionResult(bool Success, string? VitalsJson, string? Error)
{
	public static OpenAiVitalsTranscriptionResult Ok(string vitalsJson) => new(true, vitalsJson, null);

	public static OpenAiVitalsTranscriptionResult Fail(string error) => new(false, null, error);
}
