using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using SaphMovel.Models;

namespace SaphMovel.ViewModels;

public sealed class IdentificacaoViewModel : ObservableModel
{
	readonly MainViewModel _main;
	readonly Page _page;

	public IdentificacaoFormData Form => _main.Identificacao;

	public string PatientEmoji => _main.PatientEmoji;
	public string PatientSummary => _main.PatientSummary;

	public ObservableCollection<string> SexoItems { get; } = new(["", "Feminino", "Masculino"]);

	public ObservableCollection<string> CorItems { get; } =
		new(["", "Branca", "Preta", "Parda", "Amarela", "Indígena"]);

	public ICommand VoltarCommand { get; }
	public ICommand EnviarCommand { get; }

	public IdentificacaoViewModel(MainViewModel main, Page page)
	{
		_main = main;
		_page = page;
		VoltarCommand = new Command(async () => await VoltarAsync());
		EnviarCommand = new Command(async () => await EnviarAsync());
		_main.Identificacao.PropertyChanged += OnFormPropertyChanged;
		_main.PropertyChanged += OnMainPropertyChanged;
	}

	void OnMainPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is null || e.PropertyName == nameof(MainViewModel.PatientSummary))
			OnPropertyChanged(nameof(PatientSummary));
	}

	public void Detach()
	{
		_main.Identificacao.PropertyChanged -= OnFormPropertyChanged;
		_main.PropertyChanged -= OnMainPropertyChanged;
	}

	public void RefreshSexoPicker() => OnPropertyChanged(nameof(SexoPickerItem));

	void OnFormPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(IdentificacaoFormData.Sexo))
			OnPropertyChanged(nameof(SexoPickerItem));
	}

	/// <summary>Texto do Picker de sexo (ligado a <see cref="IdentificacaoFormData.Sexo"/>).</summary>
	public string SexoPickerItem
	{
		get => Form.Sexo switch { "F" => "Feminino", "M" => "Masculino", _ => "" };
		set
		{
			var code = value == "Feminino" ? "F" : value == "Masculino" ? "M" : "";
			if (Form.Sexo == code)
				return;
			Form.Sexo = code;
			OnPropertyChanged(nameof(SexoPickerItem));
		}
	}

	async Task VoltarAsync() => await _page.Navigation.PopModalAsync();

	async Task EnviarAsync()
	{
		var err = Validar();
		if (err is not null)
		{
			await _page.DisplayAlertAsync("Identificação", err, "OK");
			return;
		}

		_main.MarcarIdentificacaoBoletimConcluido();
		await _page.DisplayAlertAsync("Identificação", "Dados guardados em memória.", "OK");
		await _page.Navigation.PopModalAsync();
	}

	string? Validar()
	{
		if (string.IsNullOrWhiteSpace(Form.Nome))
			return "Preencha o nome do paciente.";
		if (Form.Sexo is not ("F" or "M"))
			return "Selecione o sexo.";
		if (string.IsNullOrWhiteSpace(Form.IdadeAnos) || !int.TryParse(Form.IdadeAnos.Trim(), out var anos) || anos < 0)
			return "Indique a idade em anos (número válido).";
		return null;
	}
}
