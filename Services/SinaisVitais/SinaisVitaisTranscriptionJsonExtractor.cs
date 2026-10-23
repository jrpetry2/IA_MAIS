using System.Linq;
using System.Text.Json;
using SaphMovel.Services.Json;

namespace SaphMovel.Services.SinaisVitais;

/// <summary>Extrai o JSON de sinais vitais da resposta OpenAI (response_format=json → campo text, etc.).</summary>
public static class SinaisVitaisTranscriptionJsonExtractor
{
	static readonly string[] VitalsHints =
	[
		"PAD", "PAS", "FC", "FR", "Sat", "Glasgow", "RTS", "PTS", "Temp", "HGT",
		"diastólica", "diastolica", "sistólica", "sistolica", "cardíaca", "cardiaca",
		"respiratória", "respiratoria", "saturação", "saturacao", "temperatura", "glicemia",
	];

	public static bool TryExtract(string? raw, out string vitalsJson) =>
		TryExtract(raw, 0, out vitalsJson);

	static bool TryExtract(string? raw, int depth, out string vitalsJson)
	{
		vitalsJson = "";
		if (depth > LenientJson.MaxUnwrapDepth || string.IsNullOrWhiteSpace(raw))
			return false;

		var trimmed = raw.Trim();

		if (TryExtractFromJsonDocument(trimmed, depth, out vitalsJson))
			return true;

		if (LenientJson.TryUnwrapFence(trimmed, out var fenceInner))
			return TryExtract(fenceInner, depth + 1, out vitalsJson);

		return TryExtractVitalsBraceSlice(trimmed, out vitalsJson);
	}

	static bool TryExtractFromJsonDocument(string json, int depth, out string vitalsJson)
	{
		vitalsJson = "";
		try
		{
			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;

			if (LooksLikeVitalsObject(root))
			{
				vitalsJson = root.GetRawText();
				return true;
			}

			if (root.TryGetProperty("text", out var textEl))
			{
				var text = textEl.GetString();
				if (!string.IsNullOrWhiteSpace(text))
				{
					var inner = text.Trim();
					if (!string.Equals(inner, json, StringComparison.Ordinal))
						return TryExtract(inner, depth + 1, out vitalsJson);
				}
			}
		}
		catch (JsonException)
		{
			return TryExtractVitalsBraceSlice(json, out vitalsJson);
		}

		return false;
	}

	/// <summary>Objeto <c>{PAD: 80, ...}</c> (chaves sem aspas) — sem reentrar em <see cref="TryExtract(string?, out string)"/>.</summary>
	static bool TryExtractVitalsBraceSlice(string text, out string vitalsJson)
	{
		vitalsJson = "";
		if (!ContainsVitalsHint(text))
			return false;

		var start = text.IndexOf('{');
		var end = text.LastIndexOf('}');
		if (start < 0 || end <= start)
			return false;

		vitalsJson = text[start..(end + 1)];
		return true;
	}

	static bool ContainsVitalsHint(string text) =>
		SinaisVitaisKeyAliases.TextMentionsVitals(text)
		|| VitalsHints.Any(h => text.Contains(h, StringComparison.OrdinalIgnoreCase));

	static bool LooksLikeVitalsObject(JsonElement root) =>
		SinaisVitaisKeyAliases.JsonObjectHasVitalsKey(root)
		|| VitalsHints.Any(h => root.TryGetProperty(h, out _));
}
