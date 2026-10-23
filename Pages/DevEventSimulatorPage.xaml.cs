using SaphMovel.Services.Missao;
using SaphMovel.ViewModels;

namespace SaphMovel.Pages;

public partial class DevEventSimulatorPage : ContentPage
{
	public DevEventSimulatorPage(IMissaoFacade missao, MainViewModel main)
	{
		InitializeComponent();
		BindingContext = new DevEventSimulatorViewModel(missao, main, this);
	}

	protected override void OnDisappearing()
	{
		(BindingContext as DevEventSimulatorViewModel)?.Detach();
		base.OnDisappearing();
	}
}
