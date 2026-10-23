using System.Collections.ObjectModel;
using SaphMovel.Models;
using SaphMovel.Services.SinaisVitais;

namespace SaphMovel.ViewModels;

public sealed partial class MainViewModel
{
	/// <summary>Antes de qualquer “ENVIAR” em Identificação: "Não Identificado". Após sucesso: Nome + ", " + idade (anos) + " " + 1.º char do sexo (F/M).</summary>
	public string PatientSummary
	{
		get => _patientSummary;
		private set => SetField(ref _patientSummary, value);
	}

	string _patientSummary = "Não Identificado";
	string? _resumoIdNome;
	string? _resumoIdIdade;
	string? _resumoIdSexo; // "F" ou "M" — 1.º char mostrado

	public ObservableCollection<BoletimRowItem> BoletimRows { get; } = new();

	/// <summary>Formulário Identificação guardado em memória (partilhado com a página modal).</summary>
	public IdentificacaoFormData Identificacao { get; } = new();

	/// <summary>Formulário Avaliação Inicial guardado em memória.</summary>
	public AvaliacaoInicialFormData AvaliacaoInicial { get; } = new();

	/// <summary>Formulário Sinais Vitais guardado em memória.</summary>
	public SinaisVitaisFormData SinaisVitais { get; } = new();

	string? _pendingSinaisVitaisJson;

	/// <summary>JSON recebido antes de abrir a modal; aplicado em <see cref="TryConsumePendingSinaisVitaisJson"/>.</summary>
	public void QueueSinaisVitaisJson(string json) => _pendingSinaisVitaisJson = json;

	/// <summary>Aplica JSON ao formulário partilhado (ex.: integração externa).</summary>
	public bool TryApplySinaisVitaisJson(string json, out string? error) =>
		SinaisVitaisJsonMapper.TryApply(SinaisVitais, json, out error);

	/// <summary>Consome JSON em fila, se existir.</summary>
	public bool TryConsumePendingSinaisVitaisJson(out string? error)
	{
		if (string.IsNullOrWhiteSpace(_pendingSinaisVitaisJson))
		{
			error = null;
			return true;
		}

		var json = _pendingSinaisVitaisJson;
		_pendingSinaisVitaisJson = null;
		return TryApplySinaisVitaisJson(json, out error);
	}

	/// <summary>Formulário Avaliação Final guardado em memória.</summary>
	public AvaliacaoFinalFormData AvaliacaoFinal { get; } = new();

	/// <summary>Termo de Responsabilidade em memória (a linha do Boletim não usa ✓ de conclusão).</summary>
	public TermoResponsabilidadeFormData TermoResponsabilidade { get; } = new();

	/// <summary>Impressão do Boletim em memória (a linha do Boletim não usa ✓ de conclusão).</summary>
	public ImpressaoBoletimFormData ImpressaoBoletim { get; } = new();

	bool _boletimIdentificacaoConcluido;
	bool _boletimAvaliacaoInicialConcluido;
	bool _boletimSinaisVitaisConcluido;
	bool _boletimAvaliacaoFinalConcluido;

	/// <summary>Chamado após validação e “ENVIAR” na modal; mostra o ✓ na linha IDENTIFICAÇÃO.</summary>
	public void MarcarIdentificacaoBoletimConcluido()
	{
		_boletimIdentificacaoConcluido = true;
		// Snapshot (evita alterar o resumo com edições ainda não guardadas)
		_resumoIdNome = (Identificacao.Nome ?? "").Trim();
		_resumoIdIdade = (Identificacao.IdadeAnos ?? "").Trim();
		_resumoIdSexo = (Identificacao.Sexo ?? "").Trim();
		ResumoAtualDoPaciente();
		OnPropertyChanged(nameof(ChamadoPatientLine));
		foreach (var r in BoletimRows)
		{
			if (r.StepKey == BoletimStepKeys.Identificacao)
			{
				r.IsCompleted = true;
				break;
			}
		}
	}

	/// <summary>Recalcula o texto exibido acima dos passos do Boletim a partir do snapshot de identificação.</summary>
	public void ResumoAtualDoPaciente()
	{
		if (!_boletimIdentificacaoConcluido)
		{
			PatientSummary = "Não Identificado";
			return;
		}

		var nome = _resumoIdNome ?? "";
		var idade = _resumoIdIdade ?? "";
		var sec = string.IsNullOrEmpty(_resumoIdSexo) ? "" : _resumoIdSexo[0].ToString();
		PatientSummary = $"{nome}, {idade} {sec}".TrimEnd();
	}

	public void MarcarAvaliacaoInicialBoletimConcluido()
	{
		_boletimAvaliacaoInicialConcluido = true;
		foreach (var r in BoletimRows)
		{
			if (r.StepKey == BoletimStepKeys.AvaliacaoInicial)
			{
				r.IsCompleted = true;
				break;
			}
		}
	}

	public void MarcarSinaisVitaisBoletimConcluido()
	{
		_boletimSinaisVitaisConcluido = true;
		foreach (var r in BoletimRows)
		{
			if (r.StepKey == BoletimStepKeys.SinaisVitais)
			{
				r.IsCompleted = true;
				break;
			}
		}
	}

	public void MarcarAvaliacaoFinalBoletimConcluido()
	{
		_boletimAvaliacaoFinalConcluido = true;
		foreach (var r in BoletimRows)
		{
			if (r.StepKey == BoletimStepKeys.AvaliacaoFinal)
			{
				r.IsCompleted = true;
				break;
			}
		}
	}

	/// <summary>
	/// Descarta tudo o que pertence ao atendimento em curso (desvio de missão): fala, pergunta sim/não,
	/// modais abertos, boletim, paciente e prescrições.
	/// </summary>
	void ResetAtendimento()
	{
		CancelAssistantSpeech();
		Interlocked.Increment(ref _protocolConfirmGeneration);
		_ = CloseOpenModalsAsync();

		_boletimIdentificacaoConcluido = false;
		_boletimAvaliacaoInicialConcluido = false;
		_boletimSinaisVitaisConcluido = false;
		_boletimAvaliacaoFinalConcluido = false;
		_resumoIdNome = null;
		_resumoIdIdade = null;
		_resumoIdSexo = null;
		_pendingSinaisVitaisJson = null;

		Identificacao.Reset();
		AvaliacaoInicial.Reset();
		SinaisVitais.Reset();
		AvaliacaoFinal.Reset();
		TermoResponsabilidade.Reset();
		ImpressaoBoletim.Reset();

		ResumoAtualDoPaciente();
		FillBoletimRows();
		OnPropertyChanged(nameof(ChamadoPatientLine));

		FillPrescricaoDados();
	}

	/// <summary>Fecha os formulários abertos por cima da página principal para a equipa ver o novo chamado.</summary>
	static async Task CloseOpenModalsAsync()
	{
		var page = UiDialogHost.GetPage();
		if (page is null)
			return;
		try
		{
			while (page.Navigation.ModalStack.Count > 0)
				await page.Navigation.PopModalAsync(animated: false);
		}
		catch
		{
			/* modal já fechado pelo utilizador */
		}
	}

	void FillBoletimRows()
	{
		BoletimRows.Clear();
		BoletimRows.Add(new BoletimRowItem
		{
			Title = "IDENTIFICAÇÃO",
			StepKey = BoletimStepKeys.Identificacao,
			IsActionRow = false,
			IsCompleted = _boletimIdentificacaoConcluido,
		});
		BoletimRows.Add(new BoletimRowItem
		{
			Title = "AVALIAÇÃO INICIAL",
			StepKey = BoletimStepKeys.AvaliacaoInicial,
			IsActionRow = false,
			IsCompleted = _boletimAvaliacaoInicialConcluido,
		});
		BoletimRows.Add(new BoletimRowItem
		{
			Title = "SINAIS VITAIS",
			StepKey = BoletimStepKeys.SinaisVitais,
			IsActionRow = false,
			IsCompleted = _boletimSinaisVitaisConcluido,
		});
		BoletimRows.Add(new BoletimRowItem
		{
			Title = "AVALIAÇÃO FINAL",
			StepKey = BoletimStepKeys.AvaliacaoFinal,
			IsActionRow = false,
			IsCompleted = _boletimAvaliacaoFinalConcluido,
		});
		BoletimRows.Add(new BoletimRowItem { Title = "TERMO DE RESPONSABILIDADE", StepKey = BoletimStepKeys.Termo, IsActionRow = true });
		BoletimRows.Add(new BoletimRowItem { Title = "IMPRESSÃO DO BOLETIM", StepKey = BoletimStepKeys.Impressao, IsActionRow = true });
	}
}
