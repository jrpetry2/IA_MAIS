using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace SaphMovel.Theming;

/// <summary>
/// Tema claro/escuro com persistência em <see cref="Preferences"/> (armazenamento do app, sobrevive a reinstalos/updates conforme o SO).
/// </summary>
public static class SaphThemeController
{
	public const string PreferenceKey = "saph_prefer_dark";

	public static bool IsDark { get; private set; }

	/// <summary>Carrega a preferência e aplica. Se o MAUI ainda não tiver <see cref="Application.Current"/>, repita com <see cref="EnsureApplicationThemePushed"/> em <c>OnStart</c>.</summary>
	public static void InitializeFromPreference()
	{
		Apply(Preferences.Get(PreferenceKey, false), persist: false);
	}

	/// <summary>Chamado no <c>OnStart</c> se o arranque não tiver conseguido aplicar o tema (ex.: <see cref="Application.Current" /> ainda nulo no construtor).</summary>
	public static void EnsureApplicationThemePushed()
	{
		if (_themeResourcesPushed)
			return;
		if (Application.Current is null)
			return;
		PushThemeToApplicationAndNotify();
	}

	static bool _themeResourcesPushed;

	public static void Apply(bool dark, bool persist = true)
	{
		if (persist)
			Preferences.Set(PreferenceKey, dark);

		IsDark = dark;
		PushThemeToApplicationAndNotify();
	}

	static void CopyThemedColorResourcesToApp(Application app, bool dark)
	{
		var palette = dark ? SaphThemePalette.Dark : SaphThemePalette.Light;
		foreach (var kv in palette)
			app.Resources[kv.Key] = kv.Value;
	}

	static void PushThemeToApplicationAndNotify()
	{
		var app = Application.Current;
		if (app is null)
			return;

		_themeResourcesPushed = true;
		var dark = IsDark;
		app.UserAppTheme = dark ? AppTheme.Dark : AppTheme.Light;
		CopyThemedColorResourcesToApp(app, dark);
		ThemeChanged?.Invoke(null, EventArgs.Empty);
		SyncAndroidChrome(dark);
	}

	/// <summary>
	/// Reatribui as cores do tema a <c>Application.Current.Resources</c>, <b>sem</b> alterar
	/// <see cref="Application.UserAppTheme"/> nem o chrome do SO. O MAUI pode, ao trocar de aba,
	/// re-resolver <c>DynamicResource</c> com valores estáticos do dicionário mesclado; reforçar
	/// a paleta evita o flash a branco e a sensação de alternar claro/escuro.
	/// </summary>
	public static void ReapplyThemedColorResourcesOnly()
	{
		if (Application.Current is not { } app)
			return;
		var dark = IsDark;
		CopyThemedColorResourcesToApp(app, dark);
		ThemeChanged?.Invoke(null, EventArgs.Empty);
	}

	static void SyncAndroidChrome(bool dark)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
#if ANDROID
			if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is MainActivity activity)
				activity.ApplyChromeForTheme(dark);
#endif
		});
	}

	/// <summary>Disparado após o tema ser aplicado a <see cref="Application.Current"/> e <see cref="IsDark"/> estar actualizado.</summary>
	public static event EventHandler? ThemeChanged;
}
