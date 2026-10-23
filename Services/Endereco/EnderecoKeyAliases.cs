using System.Globalization;
using System.Text;

namespace SaphMovel.Services.Endereco;

/// <summary>Aliases PT para chaves de endereço no JSON da OpenAI.</summary>
public static class EnderecoKeyAliases
{
	public const string Logradouro = "Logradouro";
	public const string Numero = "Numero";
	public const string Bairro = "Bairro";
	public const string Cidade = "Cidade";
	public const string Referencia = "Referencia";

	static readonly Dictionary<string, string> Map = BuildMap();

	public static bool TryResolveCanonical(string key, out string canonical)
	{
		canonical = "";
		if (string.IsNullOrWhiteSpace(key))
			return false;
		var norm = NormalizeKey(key);
		if (Map.TryGetValue(norm, out var c))
		{
			canonical = c;
			return true;
		}

		return false;
	}

	static string NormalizeKey(string key)
	{
		var sb = new StringBuilder(key.Length);
		foreach (var ch in key.Trim().Normalize(NormalizationForm.FormD))
		{
			var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
			if (uc == UnicodeCategory.NonSpacingMark)
				continue;
			if (char.IsLetterOrDigit(ch))
				sb.Append(char.ToLowerInvariant(ch));
		}

		return sb.ToString();
	}

	static Dictionary<string, string> BuildMap()
	{
		var d = new Dictionary<string, string>(StringComparer.Ordinal);
		void Add(string canonical, params string[] aliases)
		{
			foreach (var a in aliases)
				d[NormalizeKey(a)] = canonical;
		}

		Add(Logradouro, "Logradouro", "Rua", "Avenida", "Av", "Endereco", "Endereço", "Street");
		Add(Numero, "Numero", "Número", "Num", "N", "No", "Number");
		Add(Bairro, "Bairro", "Distrito", "Neighborhood");
		Add(Cidade, "Cidade", "Municipio", "Município", "City");
		Add(Referencia, "Referencia", "Referência", "Complemento", "PontoReferencia", "Reference");
		return d;
	}
}
