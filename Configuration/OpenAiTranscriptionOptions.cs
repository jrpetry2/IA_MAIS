namespace SaphMovel.Configuration;

/// <summary>Opções para transcrição de áudio (OpenAI). <see cref="ApiKey"/> é definida no registo DI em <c>MauiProgram</c>.</summary>
public sealed class OpenAiTranscriptionOptions
{
	public const string VitalsTranscriptionPrompt = "Transcreva esta gravação de audio e monte cada item em uma string em formato json";

	public const string AddressTranscriptionPrompt =
		"Transcreva esta gravação de áudio em português do Brasil, com atenção a nomes de ruas, números, bairros e cidades.";

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

	/// <summary>Prompt chat: pergunta + fala do utilizador → sim/não/indefinido.</summary>
	public const string YesNoExtractionSystemPrompt =
		"""
		Você interpreta se a resposta do utilizador é afirmativa ou negativa a uma pergunta de sim/não em português do Brasil.

		Retorne exclusivamente um JSON válido, sem explicações, sem markdown e sem texto adicional.

		Campos esperados:
		- resposta: um de "sim", "nao", "indefinido"

		Regras:
		1. Afirmações como "sim", "quero", "pode", "positivo", "afirmativo", "isso", "ok", "claro", "por favor" (quando aceitam a pergunta) mapeiam para "sim".
		2. Negações como "não", "nao", "negativo", "agora não", "depois", "dispenso", "passa" mapeiam para "nao".
		3. Se a fala for ambígua, vazia, off-topic ou misturar sim e não sem clareza, use "indefinido".
		4. Ignore erros de ASR e grafias aproximadas.
		5. Retorne apenas o JSON final, por exemplo {"resposta":"sim"}.
		""";

	/// <summary>Prompt do 2.º passo (chat): texto transcrito → JSON de endereço.</summary>
	public const string AddressExtractionSystemPrompt =
		"""
		Você é um extrator de endereço em português do Brasil.

		Extraia do texto informado os seguintes campos e retorne exclusivamente um JSON válido, sem explicações, sem markdown e sem texto adicional.

		Campos esperados:
		- Logradouro: nome da rua, avenida, travessa, estrada, etc. (sem o número)
		- Numero: número do imóvel (pode incluir letras, ex. 120A)
		- Bairro: bairro ou distrito
		- Cidade: município
		- Referencia: ponto de referência, complemento ou "perto de…"

		Regras:
		1. "rua", "avenida", "av.", "travessa", "estrada", "alameda" e o nome que se segue mapeiam para Logradouro (inclua o tipo no Logradouro quando estiver presente).
		2. "número", "nº", "n°", "num" mapeiam para Numero.
		3. "bairro" mapeia para Bairro.
		4. "cidade", "município" mapeiam para Cidade.
		5. "referência", "referencia", "perto de", "em frente", "ao lado", "complemento" mapeiam para Referencia.
		6. Se algum campo não estiver presente, retorne null para ele.
		7. Não invente dados que não estejam no texto.
		8. Retorne apenas o JSON final.
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
