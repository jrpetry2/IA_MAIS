namespace SaphEventos;

public enum DemoRemoteCommandTipo
{
	Missao = 0,
	Protocolo = 1,
	Reset = 2,
	/// <summary>Desvio de missão: substitui o chamado em curso pelo <c>Payload</c> (volta a Comunicado).</summary>
	Desvio = 3,
}

/// <summary>Identificador de protocolo enviado pelo painel de demo.</summary>
public enum DemoRemoteProtocoloId
{
	Pcr,
	Afogamento,
	Atropelamento,
}

public sealed class PostDemoEventRequest
{
	public DemoRemoteCommandTipo Tipo { get; set; }
	public EEventoMissao? Evento { get; set; }
	public ChamadoPayload? Payload { get; set; }
	public DemoRemoteProtocoloId? Protocolo { get; set; }
}

public sealed class DemoEventItem
{
	public long Seq { get; set; }
	public DateTimeOffset CreatedAtUtc { get; set; }
	public DemoRemoteCommandTipo Tipo { get; set; }
	public EEventoMissao? Evento { get; set; }
	public ChamadoPayload? Payload { get; set; }
	public DemoRemoteProtocoloId? Protocolo { get; set; }
}

public sealed class DemoEventsPollResponse
{
	public long LatestSeq { get; set; }
	public List<DemoEventItem> Events { get; set; } = new();
}

public sealed class PostDemoEventResponse
{
	public long Seq { get; set; }
	public bool Ok { get; set; } = true;
}
