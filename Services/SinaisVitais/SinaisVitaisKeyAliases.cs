using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SaphMovel.Services.Json;

namespace SaphMovel.Services.SinaisVitais;

/// <summary>Resolve chaves JSON (siglas ou texto livre) para campos canónicos de sinais vitais.</summary>
internal static class SinaisVitaisKeyAliases
{
	public const string Pas = "PAS";
	public const string Pad = "PAD";
	public const string Fc = "FC";
	public const string Fr = "FR";
	public const string Sat = "Sat";
	public const string Temp = "Temp";
	public const string Hgt = "HGT";
	public const string Glasgow = "Glasgow";
	public const string Rts = "RTS";
	public const string Pts = "PTS";
	public const string Evolucao = "Evolucao";

	static readonly Regex NonAlnumSpaceRegex = new(
		@"[^a-z0-9\s]+",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	static readonly Dictionary<string, string> AliasToCanonical = BuildAliasMap();

	public static bool TryResolveCanonical(string rawKey, out string canonical)
	{
		canonical = "";
		if (string.IsNullOrWhiteSpace(rawKey))
			return false;

		var norm = NormalizeKey(rawKey);
		if (norm.Length == 0)
			return false;

		if (AliasToCanonical.TryGetValue(norm, out var resolved))
		{
			canonical = resolved;
			return true;
		}

		return false;
	}

	public static bool TextMentionsVitals(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return false;

		var norm = NormalizeKey(text);
		foreach (var alias in AliasToCanonical.Keys)
		{
			if (alias.Length < 2)
				continue;
			if (norm.Contains(alias, StringComparison.Ordinal))
				return true;
		}

		return false;
	}

	public static bool JsonObjectHasVitalsKey(JsonElement root)
	{
		if (root.ValueKind != JsonValueKind.Object)
			return false;

		foreach (var prop in root.EnumerateObject())
		{
			if (TryResolveCanonical(prop.Name, out _))
				return true;
		}

		return false;
	}

	static string NormalizeKey(string key)
	{
		var trimmed = key.Trim().ToLowerInvariant();
		var sb = new StringBuilder(trimmed.Length);
		foreach (var c in trimmed.Normalize(NormalizationForm.FormD))
		{
			if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
				sb.Append(c);
		}

		var s = sb.ToString().Normalize(NormalizationForm.FormC);
		s = NonAlnumSpaceRegex.Replace(s, " ");
		return LenientJson.CollapseWhitespace(s).Trim();
	}

	static Dictionary<string, string> BuildAliasMap()
	{
		var map = new Dictionary<string, string>(StringComparer.Ordinal);
		void Map(string canonical, params string[] aliases)
		{
			foreach (var alias in aliases)
			{
				var norm = NormalizeKey(alias);
				if (norm.Length > 0)
					map[norm] = canonical;
			}
		}

		Map(Pas,
			"PAS", "PA S",
			"pressao sistolica", "pressao assistorica", "pressao astolica",
			"sistolica", "ps", "maxima", "pressao maxima", "pressao arterial sistolica");

		Map(Pad,
			"PAD", "PA D",
			"pressao diastolica", "diastolica", "pd", "minima", "pressao minima",
			"pressao arterial diastolica");

		Map(Fc,
			"FC", "F C",
			"frequencia cardiaca", "freq cardiaca", "freq cardíaca",
			"batimentos", "bpm", "pulso", "heart rate", "hr");

		Map(Fr,
			"FR", "F R",
			"frequencia respiratoria", "freq respiratoria", "frequencia respiração",
			"respiração", "irpm", "rpm respiratorio", "respiratory rate");

		Map(Sat,
			"Sat", "SAT", "SpO2", "SPO2",
			"saturacao", "saturação", "saturacao de oxigenio", "saturação de oxigênio",
			"oxigenio", "oxigênio", "sat o2", "sato2");

		Map(Temp,
			"Temp", "TEMP",
			"temperatura", "temp ax", "temperatura axilar", "tax");

		Map(Hgt,
			"HGT",
			"glicemia", "glicose", "hemoglicoteste", "dextro");

		Map(Glasgow,
			"Glasgow", "GLASGOW", "GCS", "gcs",
			"escala de glasgow", "glasgow coma scale");

		Map(Rts, "RTS", "rts", "revised trauma score");
		Map(Pts, "PTS", "pts", "pediatric trauma score");
        Map(Evolucao, "Evolucao", "evolucao", "evolução", "Evolução");

        return map;
	}
}
