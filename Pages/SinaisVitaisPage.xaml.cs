using SaphMovel.Platforms.Android.Ui;
using SaphMovel.Services.Audio;
using SaphMovel.Services.OpenAi;
using SaphMovel.ViewModels;

namespace SaphMovel.Pages;

public partial class SinaisVitaisPage : ContentPage
{
	readonly SinaisVitaisViewModel _vm;

	public SinaisVitaisPage(MainViewModel mainVm)
	{
		InitializeComponent();
		_vm = new SinaisVitaisViewModel(
			mainVm,
			this,
			AppServices.GetRequiredService<IPressHoldAudioRecordService>(),
			AppServices.GetRequiredService<IOpenAiTranscriptionService>());
		BindingContext = _vm;

		OuvirPressHoldAttach.Attach(OuvirPanel.PressArea, _vm.OnOuvirPressed, _vm.OnOuvirReleased);
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_vm.OnPageAppearing();
	}

	protected override void OnDisappearing()
	{
		_vm.OnPageDisappearing();
		base.OnDisappearing();
	}
}
