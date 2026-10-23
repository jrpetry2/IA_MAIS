namespace SaphMovel.Services.OpenAi;

/// <summary>Áudio → transcrição OpenAI → extração (chat) → JSON estruturado (sinais vitais ou endereço).</summary>
public interface IOpenAiTranscriptionService
{
	/// <summary>Transcreve o áudio, envia o texto ao extrator clínico e devolve JSON para o mapper.</summary>
	Task<OpenAiVitalsTranscriptionResult> TranscribeVitalsJsonAsync(
		string audioFilePath,
		CancellationToken cancellationToken = default);

	/// <summary>Transcreve o áudio e extrai JSON de endereço (Logradouro, Numero, Bairro, Cidade, Referencia).</summary>
	Task<OpenAiAddressTranscriptionResult> TranscribeAddressJsonAsync(
		string audioFilePath,
		CancellationToken cancellationToken = default);

	/// <summary>Interpreta se a fala responde sim/não à pergunta (chat JSON, sem Whisper).</summary>
	Task<OpenAiYesNoInterpretationResult> InterpretYesNoAsync(
		string pergunta,
		string falaUsuario,
		CancellationToken cancellationToken = default);
}

public enum OpenAiYesNoAnswer
{
	Indefinido = 0,
	Sim = 1,
	Nao = 2,
}

public readonly record struct OpenAiVitalsTranscriptionResult(bool Success, string? VitalsJson, string? Error)
{
	public static OpenAiVitalsTranscriptionResult Ok(string vitalsJson) => new(true, vitalsJson, null);

	public static OpenAiVitalsTranscriptionResult Fail(string error) => new(false, null, error);
}

public readonly record struct OpenAiAddressTranscriptionResult(bool Success, string? AddressJson, string? Error)
{
	public static OpenAiAddressTranscriptionResult Ok(string addressJson) => new(true, addressJson, null);

	public static OpenAiAddressTranscriptionResult Fail(string error) => new(false, null, error);
}

public readonly record struct OpenAiYesNoInterpretationResult(
	bool Success,
	OpenAiYesNoAnswer Answer,
	string? Error)
{
	public static OpenAiYesNoInterpretationResult Ok(OpenAiYesNoAnswer answer) =>
		new(true, answer, null);

	public static OpenAiYesNoInterpretationResult Fail(string error) =>
		new(false, OpenAiYesNoAnswer.Indefinido, error);
}
