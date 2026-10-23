using System.Text.Json;
using SaphMovel.Configuration;

namespace SaphMovel.Services.Voice;

/// <summary>
/// Gramática restrita para o Vosk: o reconhecedor só escolhe entre estas frases (ou «[unk]»),
/// o que aumenta muito a precisão dos comandos com o modelo «small».
/// As frases têm de usar a grafia do vocabulário do modelo (com acentos); palavras fora do vocabulário
/// são ignoradas pelo Vosk com aviso no logcat («Ignoring word missing in vocabulary»).
/// </summary>
public static class VoiceCommandGrammar
{
	public const string UnknownToken = "[unk]";

	static readonly string[] BasePhrases =
	[
		// Frase de activação
		VoskVoiceConfig.WakePhrase,
		"alô sistema",
		"sistema",

		// Protocolo PCR
		"parada cardiorrespiratória",
		"parada cardíaca",
		"parada cardio respiratória",
		"protocolo parada",
		"pê cê erre",
		"rcp",

		// Protocolo afogamento
		"afogamento",
		"afogado",
		"afogada",
		"vítima de afogamento",
		"quase afogamento",
		"submersão",

		// Protocolo atropelamento
		"atropelamento",
		"atropelado",
		"atropelada",
		"vítima atropelada",
		"pedestre atropelado",

		// Prefixos comuns
		"ativar",
		"protocolo",
		"de",
		"do",
		"da",

		// Outros comandos
		"atualizar endereço",

		// Interromper a fala
		"pare",
		"parar",
		"para",
		"stop",
		"silêncio",
		"chega",
		"basta",
		"cancela",
		"cancelar",
		"pare de falar",
		"para de falar",

		// Respostas sim / não
		"sim",
		"claro",
		"pode",
		"pode ser",
		"quero",
		"positivo",
		"afirmativo",
		"isso",
		"ok",
		"com certeza",
		"por favor",
		"não",
		"negativo",
		"agora não",
		"depois",
		"dispenso",
		"não precisa",
		"passa",
	];

	/// <summary>JSON (array de frases) para o construtor <c>Recognizer(Model, float, String)</c>.</summary>
	public static string BuildJson(IEnumerable<string> extraPhrases)
	{
		var set = new HashSet<string>(StringComparer.Ordinal);
		var list = new List<string>();
		foreach (var p in BasePhrases.Concat(extraPhrases))
		{
			var v = (p ?? "").Trim().ToLowerInvariant();
			if (v.Length > 0 && set.Add(v))
				list.Add(v);
		}
		list.Add(UnknownToken);
		return JsonSerializer.Serialize(list);
	}

	/// <summary>Remove os «[unk]» que o Vosk devolve no texto quando usa gramática.</summary>
	public static string StripUnknown(string text) =>
		string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Where(w => !string.Equals(w, UnknownToken, StringComparison.Ordinal)));
}
