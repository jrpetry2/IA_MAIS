using SaphMovel.ViewModels;

namespace SaphMovel.Pages;

public partial class IdentificacaoPage : ContentPage
{
	public IdentificacaoPage(MainViewModel mainVm)
	{
		InitializeComponent();
		BindingContext = new IdentificacaoViewModel(mainVm, this);
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is IdentificacaoViewModel vm)
			vm.RefreshSexoPicker();
	}

	protected override void OnDisappearing()
	{
		(BindingContext as IdentificacaoViewModel)?.Detach();
		base.OnDisappearing();
	}
}
