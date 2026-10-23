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
			[SaphThemeKeys.PrescricaoCardBg] = Color.FromArgb("#F2F2F7"),
			[SaphThemeKeys.ModalSurface] = Color.FromArgb("#FFFFFF"),
			[SaphThemeKeys.ModalHeaderBg] = Color.FromArgb("#E1F5F2"),
			[SaphThemeKeys.ModalHeaderText] = Color.FromArgb("#1A1A1A"),
			[SaphThemeKeys.ModalCard] = Color.FromArgb("#F2F2F7"),
			[SaphThemeKeys.ModalField] = Color.FromArgb("#EFEFF4"),
			[SaphThemeKeys.ModalFieldText] = Color.FromArgb("#111827"),
			[SaphThemeKeys.ModalPlaceholder] = Color.FromArgb("#9CA3AF"),
			[SaphThemeKeys.ModalStroke] = Color.FromArgb("#D8D8D8"),
			[SaphThemeKeys.ModalLabel] = Color.FromArgb("#424242"),
			[SaphThemeKeys.ModalFooterBg] = Color.FromArgb("#F2F2F7"),
			[SaphThemeKeys.ModalSectionHeaderBg] = Color.FromArgb("#E1F5F2"),
			[SaphThemeKeys.RequiredMark] = Color.FromArgb("#C62828"),
			[SaphThemeKeys.RecordingTint] = Color.FromArgb("#1AC62828"),
			[SaphThemeKeys.RecordingText] = Color.FromArgb("#B71C1C"),
			[SaphThemeKeys.ProcessingTint] = Color.FromArgb("#1A24B2A1"),
			[SaphThemeKeys.ProcessingText] = Color.FromArgb("#00695C"),
			[SaphThemeKeys.StatusRegulacaoCanvas] = Color.FromArgb("#FFFFFF"),
			[SaphThemeKeys.StatusRegulacaoPill] = Color.FromArgb("#F2F2F7"),
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
			[SaphThemeKeys.PrescricaoCardBg] = Color.FromArgb("#1C1C1E"),
			[SaphThemeKeys.ModalSurface] = Color.FromArgb("#0F1211"),
			[SaphThemeKeys.ModalHeaderBg] = Color.FromArgb("#000000"),
			[SaphThemeKeys.ModalHeaderText] = Color.FromArgb("#FFFFFF"),
			[SaphThemeKeys.ModalCard] = Color.FromArgb("#1E2422"),
			[SaphThemeKeys.ModalField] = Color.FromArgb("#1A1A1A"),
			[SaphThemeKeys.ModalFieldText] = Color.FromArgb("#FFFFFF"),
			[SaphThemeKeys.ModalPlaceholder] = Color.FromArgb("#6B7280"),
			[SaphThemeKeys.ModalStroke] = Color.FromArgb("#3D4A47"),
			[SaphThemeKeys.ModalLabel] = Color.FromArgb("#B0B0B0"),
			[SaphThemeKeys.ModalFooterBg] = Color.FromArgb("#0A0D0C"),
			[SaphThemeKeys.ModalSectionHeaderBg] = Color.FromArgb("#252B29"),
			[SaphThemeKeys.RequiredMark] = Color.FromArgb("#E57373"),
			[SaphThemeKeys.RecordingTint] = Color.FromArgb("#33C62828"),
			[SaphThemeKeys.RecordingText] = Color.FromArgb("#FFCDD2"),
			[SaphThemeKeys.ProcessingTint] = Color.FromArgb("#3324B2A1"),
			[SaphThemeKeys.ProcessingText] = Color.FromArgb("#B2DFDB"),
			[SaphThemeKeys.StatusRegulacaoCanvas] = Color.FromArgb("#000000"),
			[SaphThemeKeys.StatusRegulacaoPill] = Color.FromArgb("#2C2C2C"),
		};
	}
}
