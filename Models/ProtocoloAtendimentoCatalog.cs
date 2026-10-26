namespace SaphMovel.Models;

public enum ProtocoloAtendimentoId
{
	Pcr,
	Afogamento,
	Atropelamento,
}

/// <summary>Definição estável de um protocolo clínico (pacote + procedimento falável + voz).</summary>
public sealed class ProtocoloAtendimentoDefinicao
{
	public required ProtocoloAtendimentoId Id { get; init; }

	/// <summary>Nome do <see cref="PrescricaoPacote"/> (TTS curto e detecção).</summary>
	public required string NomePacote { get; init; }

	/// <summary>Título curto para toast / UI (ex.: «Afogamento»).</summary>
	public required string TituloCurto { get; init; }

	public required string ToastAtivado { get; init; }

	/// <summary>Título registado em <c>GetVoiceCommandEntries</c>.</summary>
	public required string VoiceCommandTitle { get; init; }

	/// <summary>Sinónimos normalizados (sem acentos) para matching ASR.</summary>
	public required IReadOnlyList<string> VoiceSynonyms { get; init; }

	public required Func<PrescricaoPacote> CreatePacote { get; init; }

	/// <summary>Texto contínuo lido em voz alta se o utilizador quiser relembrar o procedimento.</summary>
	public required string ProcedimentoFala { get; init; }
}

/// <summary>Catálogo dos protocolos de atendimento (PCR, afogamento, atropelamento).</summary>
public static class ProtocoloAtendimentoCatalog
{
	public const string PerguntaRelembrarProcedimento =
		"Deseja relembrar o procedimento de atendimento?";

	static readonly ProtocoloAtendimentoDefinicao PcrDefinicao = CreatePcr();
	static readonly ProtocoloAtendimentoDefinicao AfogamentoDefinicao = CreateAfogamento();
	static readonly ProtocoloAtendimentoDefinicao AtropelamentoDefinicao = CreateAtropelamento();

	public static IReadOnlyList<ProtocoloAtendimentoDefinicao> All { get; } =
	[
		PcrDefinicao,
		AfogamentoDefinicao,
		AtropelamentoDefinicao,
	];

	public static ProtocoloAtendimentoDefinicao Get(ProtocoloAtendimentoId id) =>
		All.First(p => p.Id == id);

	public static bool TryGetByVoiceCommandTitle(string title, out ProtocoloAtendimentoDefinicao? def)
	{
		def = All.FirstOrDefault(p =>
			string.Equals(p.VoiceCommandTitle, title.Trim(), StringComparison.OrdinalIgnoreCase));
		return def is not null;
	}

	public static bool IsProtocoloPacoteNome(string? nomePacote)
	{
		if (string.IsNullOrWhiteSpace(nomePacote))
			return false;
		return All.Any(p => string.Equals(p.NomePacote, nomePacote.Trim(), StringComparison.OrdinalIgnoreCase));
	}

	static ProtocoloAtendimentoDefinicao CreatePcr() => new()
	{
		Id = ProtocoloAtendimentoId.Pcr,
		NomePacote = DemoPrescricaoFactory.ProtocoloPcrNome,
		TituloCurto = "PCR",
		ToastAtivado = "Parada cardiorrespiratória ativada",
		VoiceCommandTitle = "Parada cardiorrespiratória",
		VoiceSynonyms =
		[
			"ativar parada cardiorrespiratoria",
			"activar parada cardiorrespiratoria",
			"protocolo parada cardiorrespiratoria",
			"parada cardiorrespiratoria",
			"parada cardio respiratoria",
			"parada caradiorespiratoria",
			"parada cardiorespiratoria",
			"protocolo pcr",
			"cardiorrespiratoria",
			"caradiorespiratoria",
			"parada cardiaca",
			"parada cardio",
			"protocolo parada",
			"pe ce erre",
			"p c r",
			"rcp",
		],
		CreatePacote = DemoPrescricaoFactory.Pcr,
		ProcedimentoFala =
			"""
			Lembrete PCR.
			Verifique se a vítima responde e se está a respirar.
			Se não houver sinais claros de óbito, inicie RCP e peça o DEA.
			Prepare o material de via aérea e monitorização.
			Se a parada acontecer durante o transporte, fique em local seguro, continue RCP e use o DEA se tiver.
			""",
	};

	static ProtocoloAtendimentoDefinicao CreateAfogamento() => new()
	{
		Id = ProtocoloAtendimentoId.Afogamento,
		NomePacote = DemoPrescricaoFactory.ProtocoloAfogamentoNome,
		TituloCurto = "Afogamento",
		ToastAtivado = "Protocolo de afogamento ativado",
		VoiceCommandTitle = "Afogamento",
		VoiceSynonyms =
		[
			"ativar afogamento",
			"activar afogamento",
			"protocolo afogamento",
			"afogamento",
			"vitima de afogamento",
			"submersao",
			"quase afogamento",
			"afogado",
			"afogada",
		],
		CreatePacote = DemoPrescricaoFactory.Afogamento,
		ProcedimentoFala =
			"""
			Lembrete afogamento.
			Tire a vítima da água com segurança e avalie se responde e se respira.
			Procure falta de ar, espuma na boca ou nariz, e se houve batida na cabeça.
			Se estiver grave ou inconsciente, trate como emergência e prepare suporte avançado.
			Se entrar em parada, siga o protocolo de PCR.
			""",
	};

	static ProtocoloAtendimentoDefinicao CreateAtropelamento() => new()
	{
		Id = ProtocoloAtendimentoId.Atropelamento,
		NomePacote = DemoPrescricaoFactory.ProtocoloAtropelamentoNome,
		TituloCurto = "Atropelamento",
		ToastAtivado = "Protocolo de atropelamento ativado",
		VoiceCommandTitle = "Atropelamento",
		VoiceSynonyms =
		[
			"ativar atropelamento",
			"activar atropelamento",
			"protocolo atropelamento",
			"atropelamento",
			"vitima atropelada",
			"pedestre atropelado",
			"trauma por atropelamento",
			"atropelado",
			"atropelada",
		],
		CreatePacote = DemoPrescricaoFactory.Atropelamento,
		ProcedimentoFala =
			"""
			Lembrete atropelamento.
			Proteja o local e avalie respiração, sangramentos e se a vítima responde.
			Controle sangramento visível com compressão e pano limpo.
			Observe sinais de trauma na cabeça, alterações neurológicas e fraturas.
			Se estiver grave, prepare suporte avançado.
			Se estiver presa nas ferragens, peça apoio dos Bombeiros e unidade especializada; a remoção precisa de ambulância de suporte básico.
			""",
	};
}
