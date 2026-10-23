using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using SaphMovel.Theming;

namespace SaphMovel.ViewModels;

public sealed partial class MainViewModel
{
	/// <summary>
	/// Fundo do corpo: em <b>modo escuro</b>, as abas Chamado/Prescrição/Local usam preto; em modo claro seguem a paleta (não forçar “dark” visual com preferência clara).
	/// </summary>
	public Color MainContentAreaBackgroundColor
	{
		get
		{
			if (SaphThemeController.IsDark && _selectedTabKey is "CHAMADO" or "PRESCRICAO" or "LOCALIZACAO")
				return Color.FromArgb("#000000");
			return TemaAtual()[SaphThemeKeys.BodySurface];
		}
	}

	/// <summary>Cor de corpo de cartão/lista (SaphBodyWhite) — use binding em XAML em vez de DynamicResource para alinhar ao <see cref="SaphThemePalette"/> sem atraso.</summary>
	public Color ThemedBodySurface => TemaAtual()[SaphThemeKeys.BodySurface];

	/// <summary>Aba CHAMADO. Cromo preto (Figma) <b>só</b> com tema escolhido = escuro; com modo claro, o cabeçalho/ abas alinham ao resto da app.</summary>
	public bool IsChamadoTab => string.Equals(_selectedTabKey, "CHAMADO", StringComparison.Ordinal);

	/// <summary>Estilo Figma (preto + texto claro) só se o utilizador tiver o tema escuro activo; evita “o CHAMADO parece pôr tudo a dark” em modo claro.</summary>
	bool IsChamadoTabDarkFigma => IsChamadoTab && SaphThemeController.IsDark;

	public Color MainChromeBackgroundColor =>
		IsChamadoTabDarkFigma
			? Color.FromArgb("#000000")
			: TemaAtual()[SaphThemeKeys.BoletimHeaderMint];

	public Color MainChromePrimaryTextColor =>
		IsChamadoTabDarkFigma
			? Color.FromArgb("#FFFFFF")
			: TemaAtual()[SaphThemeKeys.BoletimTitle];

	public Color MainChromeSecondaryTextColor =>
		IsChamadoTabDarkFigma
			? Color.FromArgb("#8E8E93")
			: TemaAtual()[SaphThemeKeys.BoletimSubtitle];

	public Color MainChromeTabDividerColor =>
		IsChamadoTabDarkFigma
			? Color.FromArgb("#3A3A3C")
			: TemaAtual()[SaphThemeKeys.BoletimTabDivider];

	/// <summary>Pílula de estado (CHAMADO) — fica escura com layout Figma; com modo claro, cinzento alinhado ao claro.</summary>
	public Color ChamadoPillBoxBackground =>
		IsChamadoTab && SaphThemeController.IsDark
			? Color.FromArgb("#2C2C2C")
			: Color.FromArgb("#E5E5E5");

	/// <summary>Cor semântica de <c>Colors.xaml</c>; o hex é só fallback se o dicionário ainda não estiver carregado.</summary>
	static Color ResourceColor(string key, string fallbackHex) =>
		Application.Current?.Resources?.TryGetValue(key, out var value) == true && value is Color color
			? color
			: Color.FromArgb(fallbackHex);

	static IReadOnlyDictionary<string, Color> TemaAtual() =>
		SaphThemeController.IsDark ? SaphThemePalette.Dark : SaphThemePalette.Light;

	void OnSaphThemeChanged(object? sender, EventArgs e)
	{
		if (Application.Current?.Dispatcher is { } d)
			d.Dispatch(RefreshThemeBoundChromeColors);
		else
			RefreshThemeBoundChromeColors();
	}

	void RefreshThemeBoundChromeColors()
	{
		OnPropertyChanged(nameof(MainContentAreaBackgroundColor));
		OnPropertyChanged(nameof(MainChromeBackgroundColor));
		OnPropertyChanged(nameof(MainChromePrimaryTextColor));
		OnPropertyChanged(nameof(MainChromeSecondaryTextColor));
		OnPropertyChanged(nameof(MainChromeTabDividerColor));
		OnPropertyChanged(nameof(ThemedBodySurface));
		OnPropertyChanged(nameof(ChamadoPillBoxBackground));
	}
}
