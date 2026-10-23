using SaphMovel.Services.Voice;
using SaphMovel.ViewModels;

namespace SaphMovel.Pages;

public partial class TtsVoicePickerPage : ContentPage
{
	public TtsVoicePickerPage(IChamadoSpeechService speech)
	{
		InitializeComponent();
		BindingContext = new TtsVoicePickerViewModel(speech, this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (BindingContext is TtsVoicePickerViewModel vm)
			await vm.LoadAsync();
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
	}
}
