using SaphMovel.ViewModels;

namespace SaphMovel.Pages;

public partial class AvaliacaoInicialPage : ContentPage
{
	public AvaliacaoInicialPage(MainViewModel mainVm)
	{
		InitializeComponent();
		BindingContext = new AvaliacaoInicialViewModel(mainVm, this);
	}
}
