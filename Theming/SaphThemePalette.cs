using Microsoft.Maui.Graphics;

namespace SaphMovel.Theming;

/// <summary>Valores de cor por tema (claro / escuro). Cores de estado (verde, vermelho, teal) ficam nos recursos estáticos do XAML.</summary>
public static class SaphThemePalette
{
	public static IReadOnlyDictionary<string, Color> Light { get; }
	public static IReadOnlyDictionary<string, Color> Dark { get; }

	static SaphThemePalette()
	{
		Light = new Dictionary<string, Color>(StringComparer.Ordinal)
		{
			[SaphThemeKeys.BoletimHeaderMint] = Color.FromArgb("#E1F5F2"),
			[SaphThemeKeys.BoletimTitle] = Color.FromArgb("#1A1A1A"),
			[SaphThemeKeys.BoletimSubtitle] = Color.FromArgb("#424242"),
			[SaphThemeKeys.BodySurface] = Color.FromArgb("#FFFFFF"),
			[SaphThemeKeys.BoletimTabDivider] = Color.FromArgb("#CFCFCF"),
			[SaphThemeKeys.ListDivider] = Color.FromArgb("#E0E0E0"),
			[SaphThemeKeys.ListTitle] = Color.FromArgb("#1A1A1A"),
			[SaphThemeKeys.BoletimCardStroke] = Color.FromArgb("#E0E0E0"),
			[SaphThemeKeys.BoletimSoftRow] = Color.FromArgb("#E1F5F2"),
			[SaphThemeKeys.StatusPillStroke] = Color.FromArgb("#E0E0E0"),
			[SaphThemeKeys.StatusPrescriptionCard] = Color.FromArgb("#E1F5F2"),
			[SaphThemeKeys.TextSecondary] = Color.FromArgb("#6B7280"),
			[SaphThemeKeys.Accent] = Color.FromArgb("#111827"),
			[SaphThemeKeys.OnAccent] = Color.FromArgb("#FFFFFF"),
			[SaphThemeKeys.OnActionButton] = Color.FromArgb("#FFFFFF"),
		};

		Dark = new Dictionary<string, Color>(StringComparer.Ordinal)
		{
			[SaphThemeKeys.BoletimHeaderMint] = Color.FromArgb("#0C1614"),
			[SaphThemeKeys.BoletimTitle] = Color.FromArgb("#EDEDED"),
			[SaphThemeKeys.BoletimSubtitle] = Color.FromArgb("#A8A8A8"),
			[SaphThemeKeys.BodySurface] = Color.FromArgb("#0E0E10"),
			[SaphThemeKeys.BoletimTabDivider] = Color.FromArgb("#2A3532"),
			[SaphThemeKeys.ListDivider] = Color.FromArgb("#2F3A38"),
			[SaphThemeKeys.ListTitle] = Color.FromArgb("#F2F2F2"),
			[SaphThemeKeys.BoletimCardStroke] = Color.FromArgb("#3D4A47"),
			[SaphThemeKeys.BoletimSoftRow] = Color.FromArgb("#152824"),
			[SaphThemeKeys.StatusPillStroke] = Color.FromArgb("#3D4A47"),
			[SaphThemeKeys.StatusPrescriptionCard] = Color.FromArgb("#122620"),
			[SaphThemeKeys.TextSecondary] = Color.FromArgb("#9CA3AF"),
			[SaphThemeKeys.Accent] = Color.FromArgb("#E5E7EB"),
			[SaphThemeKeys.OnAccent] = Color.FromArgb("#111827"),
			[SaphThemeKeys.OnActionButton] = Color.FromArgb("#FFFFFF"),
		};
	}
}
