using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;
using SaphMovel.Services.SinaisVitais;
using SaphMovel.Theming;

namespace SaphMovel;

[Activity(Theme = "@style/Maui.SplashTheme.Saph", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	/// <summary>Arranque: splash com fundo fixo (estilo Saph) — tema da app aplica-se depois via MAUI / <see cref="SaphThemeController"/>.</summary>
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);
		ApplyChromeForTheme(SaphThemeController.IsDark);
		DeliverSinaisVitaisJsonIfPresent(Intent);
	}

	protected override void OnNewIntent(Intent? intent)
	{
		base.OnNewIntent(intent);
		if (intent is not null)
			Intent = intent;
		DeliverSinaisVitaisJsonIfPresent(intent);
	}

	static void DeliverSinaisVitaisJsonIfPresent(Intent? intent)
	{
		var json = intent?.GetStringExtra(SinaisVitaisJsonMapper.AndroidIntentExtraName);
		if (string.IsNullOrWhiteSpace(json))
			return;
		SinaisVitaisDelivery.PublishJson(json);
	}

	/// <summary>Barras de sistema alinhadas ao tema (chamado também ao alternar claro/escuro).</summary>
	public void ApplyChromeForTheme(bool dark)
	{
		if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop || Window is null)
			return;

		if (dark)
		{
			Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#0C1614"));
			Window.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#0E0E10"));
		}
		else
		{
			Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#E1F5F2"));
			Window.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#FFFFFF"));
		}

		var decor = Window.DecorView;
		if (decor is null)
			return;

		var insets = WindowCompat.GetInsetsController(Window, decor);
		if (insets is null)
			return;

		// Fundos claros → ícones escuros; fundos escuros → ícones claros
		insets.AppearanceLightStatusBars = !dark;
		insets.AppearanceLightNavigationBars = !dark;
	}
}
