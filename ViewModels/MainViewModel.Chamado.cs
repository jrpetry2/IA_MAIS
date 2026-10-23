using Microsoft.Maui.Controls;
using SaphEventos;
using SaphMovel.Pages;
using SaphMovel.Services.Missao;

namespace SaphMovel.ViewModels;

public sealed partial class MainViewModel
{
	// Aba CHAMADO (alimentada pelo IMissaoFacade / simulador)
	string _chamadoEndereco = "—";
	string _chamadoReferencia = "—";
	string _chamadoLogradouro = "";
	string _chamadoNumero = "";
	string _chamadoBairro = "";
	string _chamadoCidade = "";
	string _chamadoAcionamento = "—";
	string _chamadoNumeroOcorrencia = "—";
	string _chamadoAtendimento = "—";
	string _chamadoOrientacaoRegulacao = "—";
	string _chamadoApoioPor = "—";
	string _chamadoTipoCliente = "—";
	string _chamadoCodigoCliente = "—";
	string _chamadoDescricaoEvento = "Disponível";
	string _chamadoDataHoraEvento = "—";
	string _chamadoPacienteLinha = "—";
	EEventoMissao? _ultimoEventoMissaoAplicado;

	public string ChamadoPillText => string.IsNullOrWhiteSpace(_chamadoDescricaoEvento)
		? RegulationStatus
		: _chamadoDescricaoEvento;
	public string ChamadoEquipeApoioLabel => "Informações para Equipe de Apoio";
	public string ChamadoEndereco => _chamadoEndereco;
	public string ChamadoReferencia => _chamadoReferencia;
	public string ChamadoLogradouro => _chamadoLogradouro;
	public string ChamadoNumero => _chamadoNumero;
	public string ChamadoBairro => _chamadoBairro;
	public string ChamadoCidade => _chamadoCidade;
	public string ChamadoAcionamento => _chamadoAcionamento;
	public string ChamadoNumeroOcorrencia => _chamadoNumeroOcorrencia;
	public string ChamadoAtendimento => _chamadoAtendimento;
	public string ChamadoOrientacaoRegulacao => _chamadoOrientacaoRegulacao;
	public string ChamadoApoioPor => _chamadoApoioPor;
	public string ChamadoTipoCliente => _chamadoTipoCliente;
	public string ChamadoCodigoCliente => _chamadoCodigoCliente;
	public string ChamadoDescricaoEvento => _chamadoDescricaoEvento;
	public string ChamadoDataHoraEvento => _chamadoDataHoraEvento;
	public string ChamadoDicaTitulo => "Dica para uma Equipe Campeã!";
	public string ChamadoDicaTexto => "Registre corretamente os comandos dos status do chamado para um tempo resposta melhor.";

	/// <summary>Ex.: “Milton Petry,42 M”. Com identificação concluída, usa o snapshot do Boletim.</summary>
	public string ChamadoPatientLine
	{
		get
		{
			if (_boletimIdentificacaoConcluido)
			{
				var nome = string.IsNullOrWhiteSpace(_resumoIdNome) ? "Não identificado" : _resumoIdNome.Trim();
				var id = (_resumoIdIdade ?? "").Trim();
				var sex = (_resumoIdSexo ?? "").Trim();
				if (string.IsNullOrEmpty(id) && string.IsNullOrEmpty(sex))
					return nome;
				var sec = string.IsNullOrEmpty(sex) ? "" : char.ToUpperInvariant(sex[0]).ToString();
				return string.IsNullOrEmpty(sec) ? $"{nome} ({id})" : $"{nome} ({id} {sec})";
			}

			return _chamadoPacienteLinha;
		}
	}

	void OnMissaoChanged(MissaoSnapshot snap)
	{
		var app = Application.Current;
		if (app?.Dispatcher is { } d)
			d.Dispatch(() => ApplyMissaoSnapshot(snap));
		else
			ApplyMissaoSnapshot(snap);
	}

	void ApplyMissaoSnapshot(MissaoSnapshot snap)
	{
		var eventoAnterior = _ultimoEventoMissaoAplicado;
		var entrouEmCiente = snap.Evento == EEventoMissao.Ciente
			&& eventoAnterior != EEventoMissao.Ciente;

		// Desvio de missão: o atendimento anterior deixa de existir antes de carregar o novo chamado.
		if (snap.EhDesvio)
			ResetAtendimento();

		StatusScene = MissaoFacade.ToStatusScene(snap.Evento);
		var p = snap.Payload;
		if (p is null)
		{
			_chamadoEndereco = "—";
			_chamadoReferencia = "—";
			_chamadoLogradouro = "";
			_chamadoNumero = "";
			_chamadoBairro = "";
			_chamadoCidade = "";
			_chamadoAcionamento = "—";
			_chamadoNumeroOcorrencia = "—";
			_chamadoAtendimento = "—";
			_chamadoOrientacaoRegulacao = "—";
			_chamadoApoioPor = "—";
			_chamadoTipoCliente = "—";
			_chamadoCodigoCliente = "—";
			_chamadoDescricaoEvento = "Disponível";
			_chamadoDataHoraEvento = "—";
			_chamadoPacienteLinha = "—";
			RegulationStatus = "Disponível";
		}
		else
		{
			p.EnsureDisplayFields();
			_chamadoEndereco = p.Endereco;
			_chamadoReferencia = string.IsNullOrWhiteSpace(p.Referencia) ? "—" : p.Referencia;
			_chamadoLogradouro = p.Logradouro ?? "";
			_chamadoNumero = p.Numero ?? "";
			_chamadoBairro = p.Bairro ?? "";
			_chamadoCidade = p.Cidade ?? "";
			_chamadoAcionamento = p.Acionamento;
			_chamadoNumeroOcorrencia = p.NumeroOcorrencia;
			_chamadoAtendimento = string.IsNullOrWhiteSpace(p.Atendimento) ? p.TipoDeSocorro : p.Atendimento;
			_chamadoOrientacaoRegulacao = p.OrientacaoRegulacao;
			_chamadoApoioPor = p.ApoioPor;
			_chamadoTipoCliente = p.TipoCliente;
			_chamadoCodigoCliente = p.CodigoCliente;
			_chamadoDescricaoEvento = p.DescricaoEvento;
			_chamadoDataHoraEvento = p.DataHoraEvento;
			_chamadoPacienteLinha = p.PacienteLinha;
			RegulationStatus = string.IsNullOrWhiteSpace(p.DescricaoEvento) ? RegulationStatus : p.DescricaoEvento;
		}

		NotifyChamadoUi();
		OnPropertyChanged(nameof(ShowAtualizarEnderecoButton));
		if (snap.EhDesvio
		    || (snap.Evento == EEventoMissao.Comunicado && eventoAnterior != EEventoMissao.Comunicado))
			SelectedTabKey = "STATUS";

		_ultimoEventoMissaoAplicado = snap.Evento;

		if (snap.EhDesvio)
		{
			_ = ShowSuccessToastAsync("Desvio de missão — novo chamado recebido");
			_ = AnnounceDesvioAsync(IsVoiceMicrophoneActive ? this : null);
			return;
		}

		if (entrouEmCiente && snap.Payload is not null)
		{
			var payload = snap.Payload;
			var resumeVosk = IsVoiceMicrophoneActive ? this : null;
			_ = SpeakCienteAsync(payload, resumeVosk);
		}
	}

	const string DesvioFala =
		"Atenção equipe, desvio de missão. Novo chamado recebido. Confirme ciente para ouvir os detalhes.";

	async Task AnnounceDesvioAsync(MainViewModel? voskHost)
	{
		try
		{
			await _chamadoSpeech.SpeakTextAsync(DesvioFala, voskHost).ConfigureAwait(false);
		}
		catch
		{
			/* logado no serviço */
		}
	}

	async Task SpeakCienteAsync(ChamadoPayload payload, MainViewModel? voskHost)
	{
		try
		{
			await _chamadoSpeech.SpeakChamadoAsync(payload, voskHost).ConfigureAwait(false);
		}
		catch
		{
			/* logado no serviço */
		}
	}

	void NotifyChamadoUi()
	{
		OnPropertyChanged(nameof(ChamadoPillText));
		OnPropertyChanged(nameof(ChamadoEndereco));
		OnPropertyChanged(nameof(ChamadoReferencia));
		OnPropertyChanged(nameof(ChamadoLogradouro));
		OnPropertyChanged(nameof(ChamadoNumero));
		OnPropertyChanged(nameof(ChamadoBairro));
		OnPropertyChanged(nameof(ChamadoCidade));
		OnPropertyChanged(nameof(ChamadoAcionamento));
		OnPropertyChanged(nameof(ChamadoNumeroOcorrencia));
		OnPropertyChanged(nameof(ChamadoAtendimento));
		OnPropertyChanged(nameof(ChamadoOrientacaoRegulacao));
		OnPropertyChanged(nameof(ChamadoApoioPor));
		OnPropertyChanged(nameof(ChamadoTipoCliente));
		OnPropertyChanged(nameof(ChamadoCodigoCliente));
		OnPropertyChanged(nameof(ChamadoDescricaoEvento));
		OnPropertyChanged(nameof(ChamadoDataHoraEvento));
		OnPropertyChanged(nameof(ChamadoPatientLine));
		OnPropertyChanged(nameof(RegulationStatus));
		OnPropertyChanged(nameof(StatusPillText));
		OnPropertyChanged(nameof(StatusHeaderContextText));
	}

	/// <summary>Visível com chamado activo (fora da lista Disponível).</summary>
	public bool ShowAtualizarEnderecoButton => !IsListaStatusScene && _missaoFacade.Atual.Payload is not null;

	/// <summary>Aplica endereço no chamado em memória (MVP sem WCF).</summary>
	public void ApplyEnderecoUpdate(
		string logradouro,
		string numero,
		string bairro,
		string cidade,
		string referencia)
	{
		// Uma só fórmula de endereço legível: a de ChamadoPayload.EnsureDisplayFields.
		var p = _missaoFacade.Atual.Payload ?? new ChamadoPayload();
		p.Logradouro = (logradouro ?? "").Trim();
		p.Numero = (numero ?? "").Trim();
		p.Bairro = (bairro ?? "").Trim();
		p.Cidade = (cidade ?? "").Trim();
		p.Referencia = (referencia ?? "").Trim();
		p.Endereco = "";
		p.EnsureDisplayFields();

		var snap = _missaoFacade.Atual;
		if (snap.Payload is null)
		{
			_chamadoLogradouro = p.Logradouro;
			_chamadoNumero = p.Numero;
			_chamadoBairro = p.Bairro;
			_chamadoCidade = p.Cidade;
			_chamadoReferencia = string.IsNullOrWhiteSpace(p.Referencia) ? "—" : p.Referencia;
			_chamadoEndereco = string.IsNullOrWhiteSpace(p.Endereco) ? "—" : p.Endereco;
			NotifyChamadoUi();
			return;
		}

		_missaoFacade.Publish(snap.Evento, p);
	}

	async Task OpenAlterarEnderecoAsync()
	{
		var page = UiDialogHost.GetPage();
		if (page is null)
			return;
		if (_missaoFacade.Atual.Payload is null)
		{
			await page.DisplayAlertAsync(
				"Alterar Endereço",
				"Não há chamado activo. Use o simulador para criar um chamado primeiro.",
				"OK");
			return;
		}

		await page.Navigation.PushModalAsync(new AlterarEnderecoPage(this));
	}

	async Task VisualizarUltimoRegistroAsync()
	{
		var page = UiDialogHost.GetPage();
		if (page is not null)
			await page.DisplayAlertAsync("Equipe de Apoio", "Visualização do último registo (integração com API).", "OK");
	}
}
