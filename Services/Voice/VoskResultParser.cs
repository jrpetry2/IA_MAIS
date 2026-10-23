using System.Text.Json;

namespace SaphMovel.Services.Voice;

/// <summary>Resultado final do Vosk: texto e confiança média das palavras (quando <c>setWords(true)</c> está activo).</summary>
public readonly record struct VoskUtterance(string Text, double? AverageConfidence);

public static class VoskResultParser
{
	/// <summary>
	/// Lê <c>{"result":[{"word":"sim","conf":0.93,…}],"text":"sim"}</c>.
	/// <see cref="VoskUtterance.AverageConfidence"/> é null sem o array «result» — o filtro de confiança não se aplica.
	/// </summary>
	public static VoskUtterance? Parse(string? json)
	{
		if (string.IsNullOrWhiteSpace(json))
			return null;
		try
		{
			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;
			string? text = null;
			if (root.TryGetProperty("text", out var t))
				text = t.GetString();
			else if (root.TryGetProperty("partial", out var p))
				text = p.GetString();
			if (string.IsNullOrWhiteSpace(text))
				return null;

			double? average = null;
			if (root.TryGetProperty("result", out var words) && words.ValueKind == JsonValueKind.Array)
			{
				double sum = 0;
				var count = 0;
				foreach (var w in words.EnumerateArray())
				{
					if (w.TryGetProperty("word", out var word)
					    && string.Equals(word.GetString(), VoiceCommandGrammar.UnknownToken, StringComparison.Ordinal))
						continue;
					if (!w.TryGetProperty("conf", out var conf) || conf.ValueKind != JsonValueKind.Number)
						continue;
					sum += conf.GetDouble();
					count++;
				}
				if (count > 0)
					average = sum / count;
			}

			return new VoskUtterance(text.Trim(), average);
		}
		catch (JsonException)
		{
			return null;
		}
	}
}
