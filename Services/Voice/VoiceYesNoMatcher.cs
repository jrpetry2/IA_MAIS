namespace SaphMovel.Services.Voice;

public enum VoiceYesNoAnswer
{
	Indefinido,
	Sim,
	Nao,
}

/// <summary>
/// Interpreta localmente (offline) respostas curtas de sim/não às perguntas do assistente.
/// Só devolve <see cref="VoiceYesNoAnswer.Indefinido"/> quando a frase é ambígua — aí pode-se recorrer à IA.
/// </summary>
public static class VoiceYesNoMatcher
{
	/// <summary>Frases afirmativas (normalizadas, sem acentos).</summary>
	public static readonly string[] YesPhrases =
	[
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
	];

	/// <summary>Frases negativas (normalizadas, sem acentos).</summary>
	public static readonly string[] NoPhrases =
	[
		"nao",
		"negativo",
		"agora nao",
		"depois",
		"dispenso",
		"nao precisa",
		"passa",
	];

	/// <summary>Palavras da própria pergunta — se aparecerem, a frase é eco do TTS e não resposta.</summary>
	static readonly string[] PromptEchoWords =
	[
		"relembrar",
		"procedimento",
		"deseja",
	];

	public static VoiceYesNoAnswer Match(string? utteranceRaw)
	{
		var n = VoiceWakeSession.Normalize(utteranceRaw ?? "");
		if (n.Length == 0)
			return VoiceYesNoAnswer.Indefinido;

		var yes = YesPhrases.Any(p => VoiceWakeSession.ContainsAsWholePhrase(n, p));
		var no = NoPhrases.Any(p => VoiceWakeSession.ContainsAsWholePhrase(n, p));
		if (yes == no)
			return VoiceYesNoAnswer.Indefinido;
		return yes ? VoiceYesNoAnswer.Sim : VoiceYesNoAnswer.Nao;
	}

	public static bool IsLikelyPromptEcho(string? utteranceRaw)
	{
		var n = VoiceWakeSession.Normalize(utteranceRaw ?? "");
		return n.Length > 0 && PromptEchoWords.Any(w => VoiceWakeSession.ContainsAsWholePhrase(n, w));
	}
}
