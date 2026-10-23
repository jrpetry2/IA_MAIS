namespace SaphEventos;

/// <summary>Eventos da sequência operacional de missão (espelho do fluxo SAPH Móvel).</summary>
public enum EEventoMissao
{
	Disponivel = 0,
	Comunicado = 1,
	Ciente = 2,
	Deslocando = 3,
	NoLocal = 4,
	RemoverPaciente = 5,
	Removendo = 6,
	NoDestino = 7,
}
