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
		return new Window(shell);
	}
}