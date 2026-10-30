using SaphMovel.Platforms.Android.Ui;
using SaphMovel.Services.Audio;
using SaphMovel.Services.OpenAi;
using SaphMovel.ViewModels;

namespace SaphMovel.Pages;

public partial class IdentificacaoPage : ContentPage
{
	readonly IdentificacaoViewModel _vm;

	public IdentificacaoPage(MainViewModel mainVm)
	{
		InitializeComponent();
		_vm = new IdentificacaoViewModel(
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
		_vm.RefreshSexoPicker();
	}

	protected override void OnDisappearing()
	{
		_vm.OnPageDisappearing();
		_vm.Detach();
		base.OnDisappearing();
	}
}
