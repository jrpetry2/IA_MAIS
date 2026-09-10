using SaphMovel.ViewModels;

namespace SaphMovel.Pages;

public partial class AvaliacaoFinalPage : ContentPage
{
	public AvaliacaoFinalPage(MainViewModel mainVm)
	{
		InitializeComponent();
		BindingContext = new AvaliacaoFinalViewModel(mainVm, this);
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is AvaliacaoFinalViewModel vm)
			AplicarRadioNaUi(vm.Form.QuestaoRadio);
	}

	void AplicarRadioNaUi(string? valor)
	{
		var v = valor?.Trim() ?? "";
		Rb1.IsChecked = v == "Radio 1";
		Rb2.IsChecked = v == "Radio 2";
		Rb3.IsChecked = v == "Radio 3";
		Rb4.IsChecked = v == "Radio 4";
	}

	void OnRadioChecked(object? sender, CheckedChangedEventArgs e)
	{
		if (e.Value != true || sender is not RadioButton rb || BindingContext is not AvaliacaoFinalViewModel vm)
			return;
		var code = rb.Value switch
		{
			int n => n.ToString(),
			string s => s,
			_ => rb.Value?.ToString() ?? "",
		};
		vm.Form.QuestaoRadio = code switch
		{
			"1" => "Radio 1",
			"2" => "Radio 2",
			"3" => "Radio 3",
			"4" => "Radio 4",
			_ => "",
		};
	}
}
