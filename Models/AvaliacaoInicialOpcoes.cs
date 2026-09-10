using System.Collections.ObjectModel;

namespace SaphMovel.Models;

/// <summary>Itens dos pickers ABCD e secção Chegada (rótulos como no mock).</summary>
public static class AvaliacaoInicialOpcoes
{
	public static ObservableCollection<string> ViasAereas { get; } = new(
	[
		"",
		"PÉRVIAS",
		"OBSTRUÇÃO PARCIAL",
		"OBSTRUÇÃO TOTAL",
		"NECESSITA MANOBRA",
	]);

	public static ObservableCollection<string> Respiracao { get; } = new(
	[
		"",
		"NORMAL",
		"AUSENTE",
		"DISPNEIA LEVE",
		"DISPNEIA GRAVE",
		"TAQUIPNEIA",
	]);

	public static ObservableCollection<string> Circulacao { get; } = new(
	[
		"",
		"NORMAL",
		"CHOQUE",
		"HEMORRAGIA ATIVA",
		"PULSO FRACO",
		"PULSO AUSENTE",
	]);

	public static ObservableCollection<string> AberturaOcular { get; } = new(
	[
		"",
		"4 - ESPONTANÊA",
		"3 - A VOZ",
		"2 - À DOR",
		"1 - AUSENTE (NT)",
	]);

	public static ObservableCollection<string> RespostaMotora { get; } = new(
	[
		"",
		"6 - OBEDECE COMANDOS",
		"5 - LOCALIZA DOR",
		"4 - RETIRADA",
		"3 - FLEXORA ANORMAL",
		"2 - EXTENSORA ANORMAL",
		"1 - AUSENTE (NT)",
	]);

	public static ObservableCollection<string> RespostaVerbal { get; } = new(
	[
		"",
		"5 - ORIENTADA",
		"4 - CONFUSA",
		"3 - PALAVRAS INAPROPRIADAS",
		"2 - SONOROS INCOMPREENSÍVEIS",
		"1 - AUSENTE (NT)",
	]);
}
