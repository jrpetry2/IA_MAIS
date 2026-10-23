using System.Text.RegularExpressions;

namespace SaphMovel.Services.Json;

/// <summary>
/// Utilitários para JSON «à vontade» devolvido por modelos: blocos markdown, chaves sem aspas,
/// objectos sem chaves exteriores. Partilhado pelos extractores e mappers (vitais e endereço).
/// </summary>
public static class LenientJson
{
	/// <summary>Limite de desembrulhos encadeados (fence dentro de campo <c>text</c>, etc.).</summary>
	public const int MaxUnwrapDepth = 5;

	public static readonly Regex FenceRegex = new(
		@"```(?:json)?\s*([\s\S]*?)\s*```",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled,
		TimeSpan.FromMilliseconds(200));

	static readonly Regex UnquotedKeyRegex = new(
		@"([{,]\s*)([A-Za-z_][A-Za-z0-9_]*)\s*:",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	/// <summary>Fecha o objecto se faltarem <c>{}</c> e põe aspas nas chaves (<c>{PAD: 80}</c> → <c>{"PAD": 80}</c>).</summary>
	public static string NormalizeToStandardJson(string raw)
	{
		var s = raw.Trim();
		if (s.Length == 0)
			return s;
		if (!s.StartsWith('{'))
			s = "{" + s;
		if (!s.EndsWith('}'))
			s += "}";
		return UnquotedKeyRegex.Replace(s, m => $"{m.Groups[1].Value}\"{m.Groups[2].Value}\":");
	}

	/// <summary>Conteúdo de um bloco markdown, se existir e for diferente do texto original.</summary>
	public static bool TryUnwrapFence(string text, out string inner)
	{
		inner = "";
		if (!text.Contains("```", StringComparison.Ordinal))
			return false;

		var fence = FenceRegex.Match(text);
		if (!fence.Success)
			return false;

		var candidate = fence.Groups[1].Value.Trim();
		if (candidate.Length == 0 || string.Equals(candidate, text, StringComparison.Ordinal))
			return false;

		inner = candidate;
		return true;
	}

	/// <summary>Colapsa espaços consecutivos num único espaço.</summary>
	public static string CollapseWhitespace(string text) => WhitespaceRegex.Replace(text, " ");
}
