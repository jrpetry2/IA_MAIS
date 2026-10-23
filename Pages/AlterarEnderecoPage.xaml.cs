using SaphMovel.Platforms.Android.Ui;
using SaphMovel.Services.Audio;
using SaphMovel.Services.OpenAi;
using SaphMovel.ViewModels;

namespace SaphMovel.Pages;

public partial class AlterarEnderecoPage : ContentPage
{
	readonly AlterarEnderecoViewModel _vm;

	public AlterarEnderecoPage(MainViewModel mainVm)
	{
		InitializeComponent();
		_vm = new AlterarEnderecoViewModel(
			mainVm,
			this,
			AppServices.GetRequiredService<IPressHoldAudioRecordService>(),
			AppServices.GetRequiredService<IOpenAiTranscriptionService>());
		BindingContext = _vm;

		OuvirPressHoldAttach.Attach(OuvirPanel.PressArea, _vm.OnOuvirPressed, _vm.OnOuvirReleased);
	}

	protected override void OnDisappearing()
	{
		_vm.OnPageDisappearing();
		base.OnDisappearing();
	}
}
