using System.Text;
using System.Text.RegularExpressions;
using SaphMovel.Models;

namespace SaphMovel.Services.Voice;

/// <summary>Monta o texto falado ao receber um pacote de prescritões.</summary>
public static class PrescricaoSpeechScriptBuilder
{
	public static string Build(PrescricaoPacote pacote, string? nomeEquipe = null)
	{
		// Protocolos clínicos: anúncio curto, sem enumerar materiais/fármacos.
		if (ProtocoloAtendimentoCatalog.IsProtocoloPacoteNome(pacote.Nome))
		{
			var nome = (pacote.Nome ?? "").Trim();
			const string prefixo = "Protocolo ";
			if (nome.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase))
				nome = nome[prefixo.Length..].Trim();
			if (string.IsNullOrWhiteSpace(nome))
				nome = "clínico";
			return $"Novas Prescrições recebidas para o protocolo {nome}.";
		}

		var equipe = string.IsNullOrWhiteSpace(nomeEquipe) ? "equipe" : nomeEquipe.Trim();
		var sb = new StringBuilder();
		sb.Append($"Olá Equipe {equipe}, nova lista de materiais prescritos. ");

		var n = 0;
		foreach (var item in pacote.Ativas)
		{
			n++;
			sb.Append($"Item {n}: {ParaFala(item.TextoLinha)}. ");
		}

		foreach (var item in pacote.SemPrescricao)
		{
			n++;
			sb.Append($"Item {n}, material avulso: {ParaFala(item.Texto)}. ");
		}

		if (n == 0)
			sb.Append("Nenhum item no pacote.");

		return sb.ToString().Trim();
	}

	/// <summary>Expande abreviaturas para o TTS nativo pronunciar melhor.</summary>
	public static string ParaFala(string texto)
	{
		if (string.IsNullOrWhiteSpace(texto))
			return "sem detalhe";

		var t = texto
			.Replace("→", " ", StringComparison.Ordinal)
			.Replace("  ", " ", StringComparison.Ordinal)
			.Trim();

		// Quantidade + unidade (ordem: plurais / formas longas primeiro quando fizer sentido)
		t = Regex.Replace(t, @"\b(\d+)\s*uns\b", "$1 unidades", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		t = Regex.Replace(t, @"\b(\d+)\s*un\b", "$1 unidade", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		t = Regex.Replace(t, @"\b(\d+)\s*amps\b", "$1 ampolas", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		t = Regex.Replace(t, @"\b(\d+)\s*amp\b", "$1 ampola", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		t = Regex.Replace(t, @"\b(\d+)\s*mls\b", "$1 mililitros", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		t = Regex.Replace(t, @"\b(\d+)\s*ml\b", "$1 mililitros", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		t = Regex.Replace(t, @"\b(\d+)\s*mgs\b", "$1 miligramas", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		t = Regex.Replace(t, @"\b(\d+)\s*mg\b", "$1 miligramas", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		// Símbolo isolado no fim (sem número colado — raríssimo, mas seguro)
		t = Regex.Replace(t, @"\bun\b", "unidade", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		t = Regex.Replace(t, @"\bamp\b", "ampola", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		return Regex.Replace(t, @"\s{2,}", " ").Trim();
	}
}
