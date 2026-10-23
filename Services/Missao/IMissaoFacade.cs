using SaphEventos;

namespace SaphMovel.Services.Missao;

/// <summary>Estado local da missão (simulador / futuro sync). Sem SAPH Cliente.</summary>
public interface IMissaoFacade
{
	MissaoSnapshot Atual { get; }

	event Action<MissaoSnapshot>? Changed;

	void Publish(EEventoMissao evento, ChamadoPayload? payload = null);

	void Reset();

	/// <summary>
	/// Desvio de missão: substitui o chamado em curso (qualquer estado) pelo novo e volta a Comunicado.
	/// O snapshot emitido traz <see cref="MissaoSnapshot.EhDesvio"/> = true.
	/// </summary>
	void Desviar(ChamadoPayload novoChamado);

	IReadOnlyList<EEventoMissao> ProximosEventos();

	/// <summary>Avança para o primeiro próximo evento válido (botão STATUS).</summary>
	bool TryAvancarProximo(out EEventoMissao aplicado);
}
