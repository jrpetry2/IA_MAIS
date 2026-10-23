using SaphEventos;

namespace SaphMovel.Services.Missao;

public sealed class MissaoFacade : IMissaoFacade
{
	readonly object _gate = new();
	MissaoSnapshot _atual = new() { Evento = EEventoMissao.Disponivel };

	public MissaoSnapshot Atual
	{
		get
		{
			lock (_gate)
				return Clone(_atual);
		}
	}

	public event Action<MissaoSnapshot>? Changed;

	public void Publish(EEventoMissao evento, ChamadoPayload? payload = null)
	{
		MissaoSnapshot snap;
		lock (_gate)
		{
			var allowed = ProximosEventosUnlocked();
			if (evento != EEventoMissao.Disponivel
			    && evento != _atual.Evento
			    && !allowed.Contains(evento))
				return;

			var keepPayload = _atual.Payload;
			if (evento == EEventoMissao.Disponivel)
				keepPayload = null;
			else if (payload is not null)
				keepPayload = ClonePayload(payload);
			else if (evento == EEventoMissao.Comunicado && keepPayload is null)
				keepPayload = DemoChamadoFactory.Create();

			if (keepPayload is not null)
				keepPayload.DescricaoEvento = DescricaoPara(evento);
			if (keepPayload is not null && evento == EEventoMissao.Comunicado)
				keepPayload.DataHoraEvento = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

			_atual = new MissaoSnapshot
			{
				Evento = evento,
				Payload = keepPayload,
			};
			snap = Clone(_atual);
		}

		Changed?.Invoke(snap);
	}

	public void Reset() => Publish(EEventoMissao.Disponivel);

	public void Desviar(ChamadoPayload novoChamado)
	{
		ArgumentNullException.ThrowIfNull(novoChamado);
		MissaoSnapshot snap;
		lock (_gate)
		{
			// Sem validação de transição: o desvio vale em qualquer estado da missão.
			var payload = ClonePayload(novoChamado);
			payload.DescricaoEvento = "Desvio de missão";
			payload.DataHoraEvento = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
			_atual = new MissaoSnapshot
			{
				Evento = EEventoMissao.Comunicado,
				Payload = payload,
			};
			snap = Clone(_atual);
			snap.EhDesvio = true;
		}

		Changed?.Invoke(snap);
	}

	public IReadOnlyList<EEventoMissao> ProximosEventos()
	{
		lock (_gate)
			return ProximosEventosUnlocked();
	}

	public bool TryAvancarProximo(out EEventoMissao aplicado)
	{
		IReadOnlyList<EEventoMissao> next;
		lock (_gate)
			next = ProximosEventosUnlocked();

		if (next.Count == 0)
		{
			aplicado = EEventoMissao.Disponivel;
			return false;
		}

		aplicado = next[0];
		Publish(aplicado);
		return true;
	}

	List<EEventoMissao> ProximosEventosUnlocked() => _atual.Evento switch
	{
		EEventoMissao.Disponivel => [EEventoMissao.Comunicado],
		EEventoMissao.Comunicado => [EEventoMissao.Ciente],
		EEventoMissao.Ciente => [EEventoMissao.Deslocando],
		EEventoMissao.Deslocando => [EEventoMissao.NoLocal],
		EEventoMissao.NoLocal => [EEventoMissao.RemoverPaciente],
		EEventoMissao.RemoverPaciente => [EEventoMissao.Removendo],
		EEventoMissao.Removendo => [EEventoMissao.NoDestino],
		EEventoMissao.NoDestino => [EEventoMissao.Disponivel],
		_ => [EEventoMissao.Disponivel],
	};

	public static string ToStatusScene(EEventoMissao evento) => evento switch
	{
		EEventoMissao.Disponivel => "Lista",
		EEventoMissao.Comunicado => "Deslocar",
		EEventoMissao.Ciente => "AguardandoPartida",
		EEventoMissao.Deslocando => "Deslocando",
		EEventoMissao.NoLocal => "AguardandoRegulacao",
		EEventoMissao.RemoverPaciente => "AguardandoRemocao",
		EEventoMissao.Removendo => "Removendo",
		EEventoMissao.NoDestino => "NoDestino",
		_ => "Lista",
	};

	static string DescricaoPara(EEventoMissao evento) => evento switch
	{
		EEventoMissao.Disponivel => "Disponível",
		EEventoMissao.Comunicado => "Deslocar",
		EEventoMissao.Ciente => "Aguardando Partida",
		EEventoMissao.Deslocando => "Deslocando",
		EEventoMissao.NoLocal => "Aguardando Regulação",
		EEventoMissao.RemoverPaciente => "Aguardando partida para Remoção",
		EEventoMissao.Removendo => "Removendo",
		EEventoMissao.NoDestino => "No Destino",
		_ => evento.ToString(),
	};

	static MissaoSnapshot Clone(MissaoSnapshot s) => new()
	{
		Evento = s.Evento,
		Payload = s.Payload is null ? null : ClonePayload(s.Payload),
		EhDesvio = s.EhDesvio,
	};

	static ChamadoPayload ClonePayload(ChamadoPayload p)
	{
		var c = new ChamadoPayload
		{
			NumeroOcorrencia = p.NumeroOcorrencia,
			PacienteLinha = p.PacienteLinha,
			Endereco = p.Endereco,
			Referencia = p.Referencia,
			Acionamento = p.Acionamento,
			Atendimento = p.Atendimento,
			OrientacaoRegulacao = p.OrientacaoRegulacao,
			ApoioPor = p.ApoioPor,
			TipoCliente = p.TipoCliente,
			CodigoCliente = p.CodigoCliente,
			DescricaoEvento = p.DescricaoEvento,
			DataHoraEvento = p.DataHoraEvento,
			Latitude = p.Latitude,
			Longitude = p.Longitude,
			NomeEquipe = p.NomeEquipe,
			Logradouro = p.Logradouro,
			Cidade = p.Cidade,
			Bairro = p.Bairro,
			Numero = p.Numero,
			NomePaciente = p.NomePaciente,
			Idade = p.Idade,
			Sexo = p.Sexo,
			TipoDeSocorro = p.TipoDeSocorro,
		};
		c.EnsureDisplayFields();
		return c;
	}
}
