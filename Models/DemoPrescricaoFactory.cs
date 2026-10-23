namespace SaphMovel.Models;

/// <summary>Pacote demo de prescritões (simula envio do Cliente).</summary>
public sealed class PrescricaoPacote
{
	public required string Nome { get; init; }
	public required IReadOnlyList<PrescricaoAtivaItem> Ativas { get; init; }
	public required IReadOnlyList<SemPrescricaoItem> SemPrescricao { get; init; }
}

/// <summary>Pacotes locais para o simulador de eventos / comandos de voz.</summary>
public static class DemoPrescricaoFactory
{
	/// <summary>Lista mostrada ao arrancar, antes de o Cliente enviar qualquer pacote.</summary>
	public static PrescricaoPacote Inicial() => new()
	{
		Nome = "Inicial",
		Ativas =
		[
			new PrescricaoAtivaItem
			{
				Id = "1",
				TituloCategoria = "TRANSPORTE",
				TextoLinha = "BOMBA INFUSÃO 1 un",
				Pendente = true,
			},
			new PrescricaoAtivaItem
			{
				Id = "2",
				TituloCategoria = "RCP + USO DO DEA",
				TextoLinha = "DESFIBRILADOR 1 un",
				Pendente = true,
			},
			new PrescricaoAtivaItem
			{
				Id = "3",
				TituloCategoria = "TRANSPORTE",
				TextoLinha = "BOMBA INFUSÃO 1 un",
				Pendente = true,
			},
			new PrescricaoAtivaItem
			{
				Id = "4",
				TituloCategoria = "RCP + USO DO DEA",
				TextoLinha = "DESFIBRILADOR 1 un",
				Pendente = true,
			},
		],
		SemPrescricao =
		[
			new SemPrescricaoItem { Id = "s1", Texto = "ADENOSINA 3mg/ml ampola 2ml 1 amp" },
			new SemPrescricaoItem { Id = "s2", Texto = "Escopolamina + dipirona 4mg/ml + 500mg/ml 1 amp" },
		],
	};

	public static PrescricaoPacote Transporte() => new()
	{
		Nome = "Transporte",
		Ativas =
		[
			NewAtiva("TRANSPORTE", "BOMBA INFUSÃO 1 un"),
			NewAtiva("TRANSPORTE", "OXIGÉNIO 1 un"),
		],
		SemPrescricao = [],
	};

	/// <summary>Nome estável do pacote PCR (TTS e detecção de protocolo).</summary>
	public const string ProtocoloPcrNome = "Protocolo PCR";

	public const string ProtocoloAfogamentoNome = "Protocolo Afogamento";

	public const string ProtocoloAtropelamentoNome = "Protocolo Atropelamento";

	/// <summary>PCR alinhado ao POPTec nº 16 / checklist USA (sem fármacos — o POPTec não especifica).</summary>
	public static PrescricaoPacote Pcr() => new()
	{
		Nome = ProtocoloPcrNome,
		Ativas =
		[
			NewAtiva("PROTOCOLO PCR", "DEA 1 un"),
			NewAtiva("PROTOCOLO PCR", "AMBÚ 1 un"),
			NewAtiva("PROTOCOLO PCR", "OXIGÉNIO 1 un"),
			NewAtiva("PROTOCOLO PCR", "MONITOR ZOLL M2 1 un"),
			NewAtiva("PROTOCOLO PCR", "VENTILADOR OXYMAG 1 un"),
			NewAtiva("PROTOCOLO PCR", "VIDEOLARINGOSCÓPIO 1 un"),
		],
		SemPrescricao = [],
	};

	/// <summary>Afogamento — POPT nº 01 (equipamentos do checklist USA; sem fármacos no POPT).</summary>
	public static PrescricaoPacote Afogamento() => new()
	{
		Nome = ProtocoloAfogamentoNome,
		Ativas =
		[
			NewAtiva("PROTOCOLO AFOGAMENTO", "MONITOR CARDIACO 1 un"),
			NewAtiva("PROTOCOLO AFOGAMENTO", "OXÍMETRO 1 un"),
			NewAtiva("PROTOCOLO AFOGAMENTO", "VENTILADOR OXYMAG 1 un"),
			NewAtiva("PROTOCOLO AFOGAMENTO", "VIA AÉREA AVANÇADA KIT 1 un"),
			NewAtiva("PROTOCOLO AFOGAMENTO", "OXIGÉNIO 1 un"),
		],
		SemPrescricao = [],
	};

	/// <summary>Atropelamento — POPT 06/09; TXA e cetamina só com indicação médica.</summary>
	public static PrescricaoPacote Atropelamento() => new()
	{
		Nome = ProtocoloAtropelamentoNome,
		Ativas =
		[
			NewAtiva("PROTOCOLO ATROPELAMENTO", "MONITOR ZOLL M2 1 un"),
			NewAtiva("PROTOCOLO ATROPELAMENTO", "VENTILADOR OXYMAG 1 un"),
			NewAtiva("PROTOCOLO ATROPELAMENTO", "VIDEOLARINGOSCÓPIO 1 un"),
			NewAtiva("PROTOCOLO ATROPELAMENTO", "ULTRASSOM KOSMOS 1 un"),
			NewAtiva("PROTOCOLO ATROPELAMENTO", "APOIO GAIA 1 un"),
		],
		SemPrescricao =
		[
			NewSem("ÁCIDO TRANEXÂMICO 1 g (SF 0,9% 10 min) + 1 g/8 h"),
			NewSem("CETAMINA analgesia EV 0,1–0,3 mg/kg"),
		],
	};

	public static PrescricaoPacote Medicamentos() => new()
	{
		Nome = "Medicamentos (sem prescritão)",
		Ativas = [],
		SemPrescricao =
		[
			NewSem("ADENOSINA 3mg/ml ampola 2ml 1 amp"),
			NewSem("Escopolamina + dipirona 4mg/ml + 500mg/ml 1 amp"),
			NewSem("SF 0,9% 500ml 1 un"),
		],
	};

	static PrescricaoAtivaItem NewAtiva(string procedimento, string textoLinha) => new()
	{
		Id = Guid.NewGuid().ToString("N")[..8],
		TituloCategoria = procedimento,
		TextoLinha = textoLinha,
		Pendente = true,
	};

	static SemPrescricaoItem NewSem(string texto) => new()
	{
		Id = Guid.NewGuid().ToString("N")[..8],
		Texto = texto,
	};
}
