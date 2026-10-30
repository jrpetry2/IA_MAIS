using System.Globalization;
using System.Text;
using System.Text.Json;
using SaphMovel.Models;
using SaphMovel.Services.Json;

namespace SaphMovel.Services.Identificacao;

/// <summary>Mapeia o JSON de identificação da OpenAI para <see cref="IdentificacaoFormData"/>.</summary>
public static class IdentificacaoJsonMapper
{
	static readonly string[] CorValues = ["Branca", "Preta", "Parda", "Amarela", "Indígena"];

	/// <summary>Aplica só os campos presentes no JSON; <paramref name="sexoSugerido"/> indica sexo deduzido pelo nome.</summary>
	public static bool TryApply(IdentificacaoFormData form, string? json, out bool sexoSugerido, out string? error)
	{
		sexoSugerido = false;
		error = null;
		if (string.IsNullOrWhiteSpace(json))
		{
			error = "JSON vazio.";
			return false;
		}

		Dictionary<string, JsonElement> fields;
		try
		{
			using var doc = JsonDocument.Parse(LenientJson.NormalizeToStandardJson(json));
			if (doc.RootElement.ValueKind != JsonValueKind.Object)
			{
				error = "Raiz JSON deve ser um objeto.";
				return false;
			}

			fields = doc.RootElement.EnumerateObject()
				.GroupBy(p => NormalizeKey(p.Name))
				.ToDictionary(g => g.Key, g => g.First().Value.Clone());
		}
		catch (JsonException)
		{
			error = "Resposta da IA sem JSON válido.";
			return false;
		}

		var applied = false;

		void Set(string key, Action<string> apply)
		{
			var text = ReadString(fields, key);
			if (string.IsNullOrWhiteSpace(text))
				return;
			apply(text.Trim());
			applied = true;
		}

		Set("nome", v => form.Nome = v);
		Set("idadeanos", v => { if (int.TryParse(v, out var n) && n >= 0) form.IdadeAnos = n.ToString(CultureInfo.InvariantCulture); });
		Set("idademeses", v => { if (int.TryParse(v, out var n) && n >= 0) form.IdadeMeses = n.ToString(CultureInfo.InvariantCulture); });
		Set("cpf", v => form.Cpf = FormatCpf(v));
		Set("cor", v => { var cor = MatchCor(v); if (cor is not null) form.Cor = cor; });
		Set("caracteristicafisica", v => form.CaracteristicaFisica = v);
		Set("nomesocial", v => form.NomeSocial = v);
		Set("nomemae", v => form.NomeMae = v);
		Set("datanascimento", v => form.DataNascimentoText = v);
		Set("alergias", v => form.Alergias = v);

		var sexo = ReadString(fields, "sexo")?.Trim().ToUpperInvariant() switch
		{
			"F" or "FEMININO" => "F",
			"M" or "MASCULINO" => "M",
			_ => null,
		};
		if (sexo is not null)
		{
			form.Sexo = sexo;
			sexoSugerido = ReadBool(fields, "sexosugerido");
			applied = true;
		}

		// Só a data foi dita: calcula a idade localmente (a IA não sabe a data de hoje).
		if (string.IsNullOrWhiteSpace(form.IdadeAnos)
		    && DateTime.TryParseExact(form.DataNascimentoText.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var nasc)
		    && nasc <= DateTime.Today)
		{
			var anos = DateTime.Today.Year - nasc.Year;
			if (nasc.Date > DateTime.Today.AddYears(-anos))
				anos--;
			form.IdadeAnos = anos.ToString(CultureInfo.InvariantCulture);
		}

		if (!applied)
			error = "Não foram reconhecidos dados de identificação na fala.";
		return applied;
	}

	static string? ReadString(Dictionary<string, JsonElement> fields, string key)
	{
		if (!fields.TryGetValue(key, out var el))
			return null;
		return el.ValueKind switch
		{
			JsonValueKind.Null or JsonValueKind.Undefined => null,
			JsonValueKind.String => el.GetString(),
			JsonValueKind.Number => el.GetRawText(),
			_ => el.ToString(),
		};
	}

	static bool ReadBool(Dictionary<string, JsonElement> fields, string key) =>
		fields.TryGetValue(key, out var el)
		&& (el.ValueKind == JsonValueKind.True
		    || (el.ValueKind == JsonValueKind.String && bool.TryParse(el.GetString(), out var b) && b));

	static string? MatchCor(string value)
	{
		var n = NormalizeKey(value);
		return CorValues.FirstOrDefault(c => NormalizeKey(c) == n || n.StartsWith(NormalizeKey(c)[..4], StringComparison.Ordinal));
	}

	static string FormatCpf(string value)
	{
		var digits = new string(value.Where(char.IsDigit).ToArray());
		return digits.Length == 11
			? $"{digits[..3]}.{digits[3..6]}.{digits[6..9]}-{digits[9..]}"
			: value;
	}

	/// <summary>Chave sem acentos, espaços, «_» ou «-», em minúsculas («Nome_Mãe» → «nomemae»).</summary>
	static string NormalizeKey(string key)
	{
		var sb = new StringBuilder(key.Length);
		foreach (var c in key.Normalize(NormalizationForm.FormD))
		{
			if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
				continue;
			if (char.IsLetterOrDigit(c))
				sb.Append(char.ToLowerInvariant(c));
		}
		return sb.ToString();
	}
}
