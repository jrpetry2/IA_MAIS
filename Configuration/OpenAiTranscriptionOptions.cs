namespace SaphMovel.Configuration;

/// <summary>Opções para transcrição de áudio (OpenAI). Defina <see cref="ApiKey"/> via variável de ambiente OPENAI_API_KEY.</summary>
public sealed class OpenAiTranscriptionOptions
{
	public const string VitalsTranscriptionPrompt = "Transcreva esta gravação de audio e monte cada item em uma string em formato json";

	/// <summary>Prompt do 2.º passo (chat): texto transcrito → JSON PAS/PAD/FC.</summary>
	public const string VitalsExtractionSystemPrompt =
        """
		Você é um extrator de dados clínicos.

		Extraia do texto informado os seguintes campos vitais e retorne exclusivamente um JSON válido, sem explicações, sem markdown e sem texto adicional.

		Campos esperados:
		- PAS: pressão arterial sistólica
		- PAD: pressão arterial diastólica
		- FC: frequência cardíaca
		- FR: frequência respiratória
		- RTS: escala de trauma revisada
		- Glasgow: escala de coma de Glasgow
		- PTS: pontuação de trauma
		- Temp: Temperatura axilar
		- HGT: glicemia capilar
		- Sat: saturação de oxigênio
		- Evolução: texto livre

		Regras:
		1. "pressão sistólica", "sistólica", "PAS", "pressão assistólica" ou variações com erro de digitação devem ser mapeadas para PAS.
		2. "pressão diastólica", "diastólica" ou "PAD" devem ser mapeadas para PAD.
		3. "frequência cardíaca", "freq cardíaca", "fc", "batimentos" ou variações sem acento devem ser mapeadas para FC.
		4. "frequência respiratória", "freq respiratória", "fr", "respiração" ou variações sem acento devem ser mapeadas para FR.
		5. "saturação", "sat", "spo2", "sato2" ou variações sem acento devem ser mapeadas para Sat.
		6. "temperatura", "temp ax", "temperatura axilar", "tax" ou variações sem acento devem ser mapeadas para Temp.
		7. "glicemia", "hgt", "glicose", "hemoglicoteste", "dextro" ou variações sem acento devem ser mapeadas para HGT.
		8. "glasgow", "gcs", "escala de glasgow" ou variações sem acento devem ser mapeadas para Glasgow.
		9. "rts", "revised trauma score" ou variações sem acento devem ser mapeadas para RTS.
		10. "pts", "pediatric trauma score" ou variações sem acento devem ser mapeadas para PTS.
		11. "evolução" ou "evolucao" devem ser mapeadas para Evolução.
		12. Os valores devem ser numéricos, exceto o campo evolução, este é um campo texto descritivo.
		13. Se algum campo não estiver presente, retorne null para ele.
		14. Não inverta PAS e PAD.
		15. Retorne apenas o JSON final.
		""";

	public string ApiKey { get; set; } = "";

	/// <summary>Modelo para <c>/v1/audio/transcriptions</c>.</summary>
	public string Model { get; set; } = "gpt-4o-transcribe";

	/// <summary>Modelo para <c>/v1/chat/completions</c> (extração estruturada).</summary>
	public string ChatModel { get; set; } = "gpt-4o-mini";

	public string Language { get; set; } = "pt";

	public string ResponseFormat { get; set; } = "json";

	public float Temperature { get; set; } = 0f;
}
