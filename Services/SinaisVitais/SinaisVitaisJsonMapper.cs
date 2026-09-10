using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using SaphMovel.Models;

namespace SaphMovel.Services.SinaisVitais;

/// <summary>
/// Mapeia JSON de sinais vitais para <see cref="SinaisVitaisFormData"/>.
/// Aceita siglas (PAD, FC), chaves sem aspas e sinónimos em português (ex. Pressão Diastólica).
/// </summary>
public static class SinaisVitaisJsonMapper
{
	static readonly Regex UnquotedKeyRegex = new(
		@"([{,]\s*)([A-Za-z_][A-Za-z0-9_]*)\s*:",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	/// <summary>Extra de Intent Android (<c>GetStringExtra</c>).</summary>
	public const string AndroidIntentExtraName = "sinais_vitais_json";

	public static bool TryApply(SinaisVitaisFormData form, string? json, out string? error)
	{
		error = null;
		if (form is null)
		{
			error = "Formulário inválido.";
			return false;
		}

		if (string.IsNullOrWhiteSpace(json))
		{
			error = "JSON vazio.";
			return false;
		}

		SinaisVitaisPayload payload;
		try
		{
			payload = ParsePayload(json);
		}
		catch (JsonException)
		{
			error = "JSON inválido. Use o formato {PAD: x, FC: x, ...} ou chaves entre aspas.";
			return false;
		}

		if (!PayloadHasAnyValue(payload))
		{
			error = "JSON sem dados reconhecidos (PAD, FC, FR, etc.).";
			return false;
		}

		ApplyPayload(form, payload);
		return true;
	}

	public static void ApplyPayload(SinaisVitaisFormData form, SinaisVitaisPayload payload)
	{
		if (payload.Pas.HasValue)
			form.Pas = FormatValue(payload.Pas.Value);
		if (payload.Pad.HasValue)
			form.Pad = FormatValue(payload.Pad.Value);
		if (payload.Fc.HasValue)
			form.Fc = FormatValue(payload.Fc.Value);
		if (payload.Fr.HasValue)
			form.Fr = FormatValue(payload.Fr.Value);
		if (payload.Sat.HasValue)
			form.SatO2 = FormatValue(payload.Sat.Value);
		if (payload.Temp.HasValue)
			form.TempAx = FormatValue(payload.Temp.Value);
		if (payload.Hgt.HasValue)
			form.Hgt = FormatValue(payload.Hgt.Value);
		if (payload.Glasgow.HasValue)
			form.Glasgow = FormatValue(payload.Glasgow.Value);
		if (payload.Rts.HasValue)
			form.Rts = FormatValue(payload.Rts.Value);
        if (payload.Pts.HasValue)
            form.Pts = FormatValue(payload.Pts.Value);
        if (!string.IsNullOrEmpty(payload.Evolucao))
            form.Evolucao = payload.Evolucao;

    }

	internal static SinaisVitaisPayload ParsePayload(string raw)
	{
		var normalized = NormalizeToStandardJson(raw);
		using var doc = JsonDocument.Parse(normalized);
		var root = doc.RootElement;
		if (root.ValueKind != JsonValueKind.Object)
			throw new JsonException("Raiz JSON deve ser um objeto.");

		var payload = new SinaisVitaisPayload();
		foreach (var prop in root.EnumerateObject())
		{
			if (!SinaisVitaisKeyAliases.TryResolveCanonical(prop.Name, out var canonical))
				continue;

			if (prop.Value.ValueKind == JsonValueKind.String)
			{
				AssignValue(payload, canonical, prop.Value.GetString());
				continue;
			}

            if (!TryReadNumber(prop.Value, out var value))
				continue;

			AssignValue(payload, canonical, value);
		}

		return payload;
	}

	static void AssignValue(SinaisVitaisPayload payload, string canonical, object value)
	{
		switch (canonical)
		{
			case SinaisVitaisKeyAliases.Pas:
				payload.Pas = (double)value;
				break;
			case SinaisVitaisKeyAliases.Pad:
				payload.Pad = (double)value;
				break;
			case SinaisVitaisKeyAliases.Fc:
				payload.Fc = (double)value;
				break;
			case SinaisVitaisKeyAliases.Fr:
				payload.Fr = (double)value;
				break;
			case SinaisVitaisKeyAliases.Sat:
				payload.Sat = (double)value;
				break;
			case SinaisVitaisKeyAliases.Temp:
				payload.Temp = (double)value;
				break;
			case SinaisVitaisKeyAliases.Hgt:
				payload.Hgt = (double)value;
				break;
			case SinaisVitaisKeyAliases.Glasgow:
				payload.Glasgow = (double)value;
				break;
			case SinaisVitaisKeyAliases.Rts:
				payload.Rts = (double)value;
				break;
            case SinaisVitaisKeyAliases.Pts:
                payload.Pts = (double)value;
                break;
            case SinaisVitaisKeyAliases.Evolucao:
                payload.Evolucao = (string)value;
                break;
        }
	}

	static bool TryReadNumber(JsonElement el, out double value)
	{
		switch (el.ValueKind)
		{
			case JsonValueKind.Null:
			case JsonValueKind.Undefined:
				value = 0;
				return false;
			case JsonValueKind.Number:
				value = el.GetDouble();
				return true;
			case JsonValueKind.String:
				var s = el.GetString();
				if (string.IsNullOrWhiteSpace(s))
				{
					value = 0;
					return false;
				}

				s = s.Trim().Replace(',', '.');
				return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
			default:
				value = 0;
				return false;
		}
	}

	static bool PayloadHasAnyValue(SinaisVitaisPayload p) =>
		p.Pas.HasValue || p.Pad.HasValue || p.Fc.HasValue || p.Fr.HasValue || p.Sat.HasValue
		|| p.Temp.HasValue || p.Hgt.HasValue || p.Glasgow.HasValue || p.Rts.HasValue || p.Pts.HasValue || !string.IsNullOrEmpty(p.Evolucao);

	internal static string NormalizeToStandardJson(string raw)
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

	static string FormatValue(double value)
	{
		if (double.IsNaN(value) || double.IsInfinity(value))
			return "";
		if (Math.Abs(value - Math.Truncate(value)) < 1e-9)
			return ((long)Math.Truncate(value)).ToString(CultureInfo.InvariantCulture);
		return value.ToString("0.##", CultureInfo.InvariantCulture);
	}
}
