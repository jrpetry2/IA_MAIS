using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using SaphMovel.Models;

namespace SaphMovel.ViewModels;

public sealed class AvaliacaoInicialViewModel : INotifyPropertyChanged
{
	readonly MainViewModel _main;
	readonly Page _page;

	public AvaliacaoInicialFormData Form => _main.AvaliacaoInicial;

	public string PatientEmoji => _main.PatientEmoji;
	public string PatientSummary => _main.PatientSummary;

	public ObservableCollection<string> OpcoesViasAereas => AvaliacaoInicialOpcoes.ViasAereas;
	public ObservableCollection<string> OpcoesRespiracao => AvaliacaoInicialOpcoes.Respiracao;
	public ObservableCollection<string> OpcoesCirculacao => AvaliacaoInicialOpcoes.Circulacao;
	public ObservableCollection<string> OpcoesAberturaOcular => AvaliacaoInicialOpcoes.AberturaOcular;
	public ObservableCollection<string> OpcoesRespostaMotora => AvaliacaoInicialOpcoes.RespostaMotora;
	public ObservableCollection<string> OpcoesRespostaVerbal => AvaliacaoInicialOpcoes.RespostaVerbal;

	public ICommand VoltarCommand { get; }
	public ICommand EnviarCommand { get; }

	public AvaliacaoInicialViewModel(MainViewModel main, Page page)
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
			await _page.DisplayAlertAsync("Avaliação Inicial", err, "OK");
			return;
		}

		_main.MarcarAvaliacaoInicialBoletimConcluido();
		await _page.DisplayAlertAsync("Avaliação Inicial", "Dados guardados em memória.", "OK");
		await _page.Navigation.PopModalAsync();
	}

	string? Validar()
	{
		if (string.IsNullOrWhiteSpace(Form.ReporteChegada))
			return "Preencha o reporte da chegada da equipe.";
		if (string.IsNullOrWhiteSpace(Form.ViasAereas))
			return "Selecione A - Vias aéreas.";
		if (string.IsNullOrWhiteSpace(Form.Respiracao))
			return "Selecione B - Respiração.";
		if (string.IsNullOrWhiteSpace(Form.Circulacao))
			return "Selecione C - Circulação.";
		if (string.IsNullOrWhiteSpace(Form.AberturaOcular))
			return "Selecione D - Abertura ocular.";
		if (string.IsNullOrWhiteSpace(Form.RespostaMotora))
			return "Selecione D - Resposta motora.";
		if (string.IsNullOrWhiteSpace(Form.RespostaVerbal))
			return "Selecione D - Resposta verbal.";
		return null;
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	void OnPropertyChanged([CallerMemberName] string? name = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
