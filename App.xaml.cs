namespace SaphMovel;

public partial class App : Microsoft.Maui.Controls.Application
{
	public App()
	{
		InitializeComponent();
		Theming.SaphThemeController.InitializeFromPreference();
	}

	protected override void OnStart()
	{
		base.OnStart();
		// Garante paleta MAUI e UserAppTheme se ainda não houve Application.Current no construtor.
		Theming.SaphThemeController.EnsureApplicationThemePushed();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var shell = AppServices.GetRequiredService<AppShell>();
		var window = new Window(shell);
		window.Stopped += OnWindowStopped;
		window.Resumed += OnWindowResumed;
		return window;
	}

	bool _voskWasListeningBeforeBackground;

	/// <summary>Em segundo plano o Android entrega silêncio ao microfone — liberta-o e recria-o limpo no regresso.</summary>
	async void OnWindowStopped(object? sender, EventArgs e)
	{
		var vosk = AppServices.Services.GetService(typeof(SaphMovel.Services.Voice.IVoskListeningService)) as SaphMovel.Services.Voice.IVoskListeningService;
		if (vosk is null)
			return;
		_voskWasListeningBeforeBackground = vosk.IsListening;
		try
		{
			await vosk.StopAsync();
		}
		catch
		{
			/* logado no serviço */
		}
	}

	async void OnWindowResumed(object? sender, EventArgs e)
	{
		if (!_voskWasListeningBeforeBackground)
			return;
		_voskWasListeningBeforeBackground = false;
		var vosk = AppServices.Services.GetService(typeof(SaphMovel.Services.Voice.IVoskListeningService)) as SaphMovel.Services.Voice.IVoskListeningService;
		if (vosk is null)
			return;
		try
		{
			await vosk.ResumeAsync();
		}
		catch
		{
			/* logado no serviço */
		}
	}
}