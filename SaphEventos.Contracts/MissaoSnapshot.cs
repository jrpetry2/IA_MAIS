namespace SaphEventos;

/// <summary>Estado atual da missão (evento + dados do chamado).</summary>
public sealed class MissaoSnapshot
{
	public EEventoMissao Evento { get; set; } = EEventoMissao.Disponivel;
	public ChamadoPayload? Payload { get; set; }

	/// <summary>True só no snapshot emitido por um desvio de missão — o app descarta o atendimento anterior.</summary>
	public bool EhDesvio { get; set; }
}
