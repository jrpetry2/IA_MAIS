using System.Windows.Input;
using Microsoft.Maui.Controls;
using SaphMovel.Models;

namespace SaphMovel.ViewModels;

public sealed class AvaliacaoFinalViewModel
{
	readonly MainViewModel _main;
	readonly Page _page;

	public AvaliacaoFinalFormData Form => _main.AvaliacaoFinal;

	public string PatientEmoji => _main.PatientEmoji;
	public string PatientSummary => _main.PatientSummary;

	public ICommand VoltarCommand { get; }
	public ICommand EnviarCommand { get; }

	public AvaliacaoFinalViewModel(MainViewModel main, Page page)
	{
		_main = main;
		_page = page;
		VoltarCommand = new Command(async () => await VoltarAsync());
		EnviarCommand = new Command(async () => await EnviarAsync());
	}

	async Task VoltarAsync() => await _page.Navigation.PopModalAsync();

	async Task EnviarAsync()
	{
		var err = Validar();
		if (err is not null)
		{
			await _page.DisplayAlertAsync("Avaliação Final", err, "OK");
			return;
		}

		_main.MarcarAvaliacaoFinalBoletimConcluido();
		await _page.DisplayAlertAsync("Avaliação Final", "Dados guardados em memória.", "OK");
		await _page.Navigation.PopModalAsync();
	}

	string? Validar()
	{
		if (string.IsNullOrWhiteSpace(Form.TextoAvaliacao))
			return "Preencha o campo Avaliação Final.";
		if (string.IsNullOrWhiteSpace(Form.QuestaoRadio))
			return "Selecione uma opção em Questão Tipo Radio.";
		if (!Form.Checkbox1 && !Form.Checkbox2 && !Form.Checkbox3 && !Form.Checkbox4)
			return "Marque pelo menos uma opção em Questão Tipo Checkbox.";
		return null;
	}
}
