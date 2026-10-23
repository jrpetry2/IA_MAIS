namespace SaphMovel.Services.Voice;

/// <summary>Detecta pedidos para interromper a fala do assistente (barge-in).</summary>
public static class VoiceStopSpeechMatcher
{
	/// <summary>Frases curtas: a transcrição inteira (normalizada) é só o comando.</summary>
	static readonly HashSet<string> ExactStops = new(StringComparer.Ordinal)
	{
		"pare",
		"parar",
		"para",
		"stop",
		"silencio",
		"chega",
		"basta",
		"cancela",
		"cancelar",
	};

	/// <summary>Frases compostas aceites dentro da fala.</summary>
	static readonly string[] PhraseStops =
	[
		"para de falar",
		"pare de falar",
		"parar de falar",
		"para a fala",
		"pare a fala",
		"assistente pare",
		"cancela a fala",
		"cancela fala",
	];

	public static bool IsStopCommand(string? utteranceRaw)
	{
		var n = VoiceWakeSession.Normalize(utteranceRaw ?? "");
		if (n.Length == 0)
			return false;

		if (ExactStops.Contains(n))
			return true;

		// Remover acentos já feito no Normalize — «silêncio» cai em «silencio».
		if (ExactStops.Contains(n.Replace(".", "", StringComparison.Ordinal)))
			return true;

		foreach (var phrase in PhraseStops)
		{
			var p = VoiceWakeSession.Normalize(phrase);
			if (p.Length == 0)
				continue;
			if (n == p || ContainsAsWholePhrase(n, p))
				return true;
		}

		return false;
	}

	static bool ContainsAsWholePhrase(string haystackNorm, string needleNorm)
	{
		if (needleNorm.Length == 0 || haystackNorm.Length < needleNorm.Length)
			return false;
		var idx = 0;
		while ((idx = haystackNorm.IndexOf(needleNorm, idx, StringComparison.Ordinal)) >= 0)
		{
			var beforeOk = idx == 0 || char.IsWhiteSpace(haystackNorm[idx - 1]);
			var after = idx + needleNorm.Length;
			var afterOk = after >= haystackNorm.Length || char.IsWhiteSpace(haystackNorm[after]);
			if (beforeOk && afterOk)
				return true;
			idx++;
		}

		return false;
	}
}
