namespace SaphMovel.Models;

/// <summary>Card de prescrição com transporte/ procedimento (estado: pendente).</summary>
public sealed class PrescricaoAtivaItem
{
	public string Id { get; init; } = string.Empty;
	public string TituloCategoria { get; init; } = string.Empty;
	/// <summary>Ex.: "→ BOMBA INFUSÃO - 1 un"</summary>
	public string DetalheLinha { get; init; } = string.Empty;
	public bool Pendente { get; init; } = true;
}
