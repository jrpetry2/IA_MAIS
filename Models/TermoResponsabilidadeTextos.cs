namespace SaphMovel.Models;

public static class TermoResponsabilidadeTextos
{
	public const string RecusaAtendimento = "Recusa Atendimento";

	public static string Para(string? tipoSelecionado) => tipoSelecionado switch
	{
		RecusaAtendimento =>
			"Eu, paciente identificado neste boletim ou responsável, sabedor que sou da necessidade de remoção pelo SAMU e assumo as responsabilidades legais, pelas quais assino este termo:",
		"Recusa Transporte" =>
			"Eu, paciente ou responsável legal, declaro estar ciente dos riscos da recusa ou adiamento do transporte sanitário proposto e assumo as consequências desta decisão.",
		"Alta no local" =>
			"Eu, paciente ou responsável, declaro receber alta assistencial no local da ocorrência, estando ciente das orientações prestadas pela equipe.",
		_ => "Selecione um termo na lista acima para visualizar o texto correspondente.",
	};
}
