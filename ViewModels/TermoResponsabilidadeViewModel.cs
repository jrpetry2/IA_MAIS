using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using SaphMovel.Models;

namespace SaphMovel.ViewModels;

public sealed class TermoResponsabilidadeViewModel
{
	readonly MainViewModel _main;
	readonly Page _page;

	public TermoResponsabilidadeFormData Form => _main.TermoResponsabilidade;

	public string PatientEmoji => _main.PatientEmoji;
	public string PatientSummary => _main.PatientSummary;

	public ObservableCollection<string> OpcoesTermo { get; } =
		new(["", TermoResponsabilidadeTextos.RecusaAtendimento, "Recusa Transporte", "Alta no local"]);

	public ICommand VoltarCommand { get; }
	public ICommand EnviarCommand { get; }
	public ICommand AdicionarAssinaturasCommand { get; }

	public TermoResponsabilidadeViewModel(MainViewModel main, Page page)
	{
		_main = main;
		_page = page;
		VoltarCommand = new Command(async () => await VoltarAsync());
		EnviarCommand = new Command(async () => await EnviarAsync());
		AdicionarAssinaturasCommand = new Command(async () => await AdicionarAssinaturasAsync());
	}

	async Task VoltarAsync() => await _page.Navigation.PopModalAsync();

	async Task AdicionarAssinaturasAsync()
	{
		Form.AssinaturasAdicionadas = true;
		await _page.DisplayAlertAsync(
			"Assinaturas",
			"Fluxo de assinaturas registado em memória (demonstração).",
			"OK");
	}

	async Task EnviarAsync()
	{
		if (string.IsNullOrWhiteSpace(Form.TermoSelecionado))
		{
			await _page.DisplayAlertAsync("Termo de Responsabilidade", "Selecione um termo na lista.", "OK");
			return;
		}

		if (!Form.AssinaturasAdicionadas)
		{
			await _page.DisplayAlertAsync(
				"Termo de Responsabilidade",
				"Toque em «ADICIONAR ASSINATURAS» antes de enviar.",
				"OK");
			return;
		}

		// Dados já estão em memória (Form); não marcar IsCompleted na linha do Boletim.
		await _page.DisplayAlertAsync("Termo de Responsabilidade", "Dados guardados em memória.", "OK");
		await _page.Navigation.PopModalAsync();
	}
}
