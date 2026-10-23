using SaphEventos;

namespace SaphMovel.Services.Voice;

/// <summary>Monta o texto falado no Ciente a partir do payload do chamado.</summary>
public static class ChamadoSpeechScriptBuilder
{
	public static string Build(ChamadoPayload p)
	{
		p.EnsureDisplayFields();

		var equipe = OrFallback(p.NomeEquipe, "unidade");
		var logradouro = OrFallback(p.Logradouro, p.Endereco, "local informado");
		var cidade = OrFallback(p.Cidade, "cidade não informada");
		var bairro = OrFallback(p.Bairro, "bairro não informado");
		var numero = OrFallback(p.Numero, "sem número");
		var paciente = OrFallback(p.NomePaciente, ExtractNome(p.PacienteLinha), "paciente não identificado");
		var idade = OrFallback(p.Idade, ExtractIdade(p.PacienteLinha), "idade não informada");
		var socorro = OrFallback(p.TipoDeSocorro, p.Atendimento, "ocorrência não especificada");

		var sexo = NormalizeSexo(p.Sexo, p.PacienteLinha);
		var (artigoPaciente, pronome) = sexo switch
		{
			"F" => ("a", "ela"),
			"M" => ("o", "ele"),
			_ => ("o", "a pessoa"),
		};

		var artigoLogradouro = ArtigoParaLogradouro(logradouro);

		if (pronome == "a pessoa")
		{
			return
				$"Olá Equipe {equipe}, o chamado é {artigoLogradouro} {logradouro} em {cidade}, " +
				$"no bairro {bairro}, número {numero}, o paciente é {paciente}, " +
				$"tem {idade} anos e está tendo um {socorro}.";
		}

		return
			$"Olá Equipe {equipe}, o chamado é {artigoLogradouro} {logradouro} em {cidade}, " +
			$"no bairro {bairro}, número {numero}, o paciente é {artigoPaciente} {paciente}, " +
			$"{pronome} tem {idade} anos e está tendo um {socorro}.";
	}

	static string ArtigoParaLogradouro(string logradouro)
	{
		var t = logradouro.Trim();
		if (t.StartsWith("Avenida", StringComparison.OrdinalIgnoreCase)
		    || t.StartsWith("Av.", StringComparison.OrdinalIgnoreCase)
		    || t.StartsWith("Rua", StringComparison.OrdinalIgnoreCase)
		    || t.StartsWith("Travessa", StringComparison.OrdinalIgnoreCase)
		    || t.StartsWith("Estrada", StringComparison.OrdinalIgnoreCase))
			return "na";
		if (t.StartsWith("Largo", StringComparison.OrdinalIgnoreCase)
		    || t.StartsWith("Beco", StringComparison.OrdinalIgnoreCase)
		    || t.StartsWith("Viaduto", StringComparison.OrdinalIgnoreCase))
			return "no";
		return "no";
	}

	static string NormalizeSexo(string sexo, string pacienteLinha)
	{
		var s = (sexo ?? "").Trim().ToUpperInvariant();
		if (s.StartsWith('F'))
			return "F";
		if (s.StartsWith('M'))
			return "M";
		var line = (pacienteLinha ?? "").Trim().ToUpperInvariant();
		if (line.EndsWith(" F") || line.EndsWith(",F") || line.Contains(" F "))
			return "F";
		if (line.EndsWith(" M") || line.EndsWith(",M") || line.Contains(" M "))
			return "M";
		return "";
	}

	static string ExtractNome(string linha)
	{
		if (string.IsNullOrWhiteSpace(linha) || linha == "—")
			return "";
		var i = linha.IndexOf(',');
		return i > 0 ? linha[..i].Trim() : linha.Trim();
	}

	static string ExtractIdade(string linha)
	{
		if (string.IsNullOrWhiteSpace(linha))
			return "";
		var parts = linha.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length < 2)
			return "";
		var rest = parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
		return rest.Length > 0 ? rest[0] : "";
	}

	static string OrFallback(params string?[] values)
	{
		foreach (var v in values)
		{
			if (!string.IsNullOrWhiteSpace(v) && v.Trim() != "—")
				return v.Trim();
		}

		return values.Length > 0 ? (values[^1] ?? "").Trim() : "";
	}
}
