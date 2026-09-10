using SaphMovel.ViewModels;

namespace SaphMovel.Pages;

public partial class TermoResponsabilidadePage : ContentPage
{
	public TermoResponsabilidadePage(MainViewModel mainVm)
	{
		InitializeComponent();
		BindingContext = new TermoResponsabilidadeViewModel(mainVm, this);
	}
}
