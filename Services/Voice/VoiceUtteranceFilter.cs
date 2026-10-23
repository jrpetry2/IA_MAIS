using SaphMovel.Configuration;

namespace SaphMovel.Services.Voice;

public enum VoiceUtteranceVerdict
{
	Aceite,
	BaixaConfianca,
	VozDistante,
}

/// <summary>Descarta frases provavelmente ditas por quem está ao redor (baixa confiança ou volume baixo).</summary>
public static class VoiceUtteranceFilter
{
	public static VoiceUtteranceVerdict Evaluate(double? confidence, double peakDbfs)
	{
		if (!VoskVoiceConfig.UtteranceFilterEnabled)
			return VoiceUtteranceVerdict.Aceite;
		if (peakDbfs < VoskVoiceConfig.MinUtteranceLevelDbfs)
			return VoiceUtteranceVerdict.VozDistante;
		if (confidence is { } c && c < VoskVoiceConfig.MinWordConfidence)
			return VoiceUtteranceVerdict.BaixaConfianca;
		return VoiceUtteranceVerdict.Aceite;
	}

	public static string Describe(VoiceUtteranceVerdict verdict) => verdict switch
	{
		VoiceUtteranceVerdict.BaixaConfianca => "baixa confiança",
		VoiceUtteranceVerdict.VozDistante => "voz distante",
		_ => "aceite",
	};

	/// <summary>RMS do bloco em dBFS (−∞ para silêncio absoluto).</summary>
	public static double RmsDbfs(short[] buffer, int length)
	{
		if (length <= 0)
			return double.NegativeInfinity;
		double sumSquares = 0;
		for (var i = 0; i < length; i++)
		{
			double s = buffer[i];
			sumSquares += s * s;
		}
		var rms = Math.Sqrt(sumSquares / length);
		return rms <= 0 ? double.NegativeInfinity : 20 * Math.Log10(rms / 32768.0);
	}
}
