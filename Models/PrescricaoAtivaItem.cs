namespace SaphMovel.Models;

/// <summary>Item de prescritão (legado: uma linha Nome + qtd + símbolo).</summary>
public sealed class PrescricaoAtivaItem
{
	public string Id { get; init; } = string.Empty;

	/// <summary>Metadado opcional do procedimento (não mostrado no card do item).</summary>
	public string TituloCategoria { get; init; } = string.Empty;

	/// <summary>Linha única estilo legado, ex.: "BOMBA INFUSÃO 1 un".</summary>
	public string TextoLinha { get; init; } = string.Empty;

	public bool Pendente { get; init; } = true;
}
