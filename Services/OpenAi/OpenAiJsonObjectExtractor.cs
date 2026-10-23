using System.Text.Json;
using SaphMovel.Services.Json;

namespace SaphMovel.Services.OpenAi;

/// <summary>Extrai o primeiro objeto JSON de uma resposta OpenAI (fence markdown, campo text, etc.).</summary>
public static class OpenAiJsonObjectExtractor
{
	public static bool TryExtract(string? raw, out string json) =>
		TryExtract(raw, 0, out json);

	static bool TryExtract(string? raw, int depth, out string json)
	{
		json = "";
		if (depth > LenientJson.MaxUnwrapDepth || string.IsNullOrWhiteSpace(raw))
			return false;

		var trimmed = raw.Trim();

		if (TryParseObject(trimmed, out json))
			return true;

		if (LenientJson.TryUnwrapFence(trimmed, out var fenceInner))
			return TryExtract(fenceInner, depth + 1, out json);

		try
		{
			using var doc = JsonDocument.Parse(trimmed);
			if (doc.RootElement.TryGetProperty("text", out var textEl))
			{
				var text = textEl.GetString()?.Trim();
				if (!string.IsNullOrWhiteSpace(text)
				    && !string.Equals(text, trimmed, StringComparison.Ordinal))
					return TryExtract(text, depth + 1, out json);
			}
		}
		catch (JsonException)
		{
			/* tentar fatia {…} abaixo */
		}

		return TryBraceSlice(trimmed, out json);
	}

	static bool TryParseObject(string candidate, out string json)
	{
		json = "";
		try
		{
			using var doc = JsonDocument.Parse(candidate);
			if (doc.RootElement.ValueKind != JsonValueKind.Object)
				return false;
			json = doc.RootElement.GetRawText();
			return true;
		}
		catch (JsonException)
		{
			return false;
		}
	}

	static bool TryBraceSlice(string text, out string json)
	{
		json = "";
		var start = text.IndexOf('{');
		var end = text.LastIndexOf('}');
		if (start < 0 || end <= start)
			return false;

		var slice = text[start..(end + 1)];
		return TryParseObject(slice, out json);
	}
}
