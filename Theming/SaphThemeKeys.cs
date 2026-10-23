namespace SaphMovel.Theming;

/// <summary>Nomes estáveis dos recursos de cor semânticos (alinhados às chaves em <c>Colors.xaml</c>).</summary>
public static class SaphThemeKeys
{
	public const string BoletimHeaderMint = "SaphBoletimHeaderMint";
	public const string BoletimTitle = "SaphBoletimTitle";
	public const string BoletimSubtitle = "SaphBoletimSubtitle";
	public const string BodySurface = "SaphBodyWhite";
	public const string BoletimTabDivider = "SaphBoletimTabDivider";
	public const string ListDivider = "SaphListDivider";
	public const string ListTitle = "SaphListTitle";
	public const string BoletimCardStroke = "SaphBoletimCardStroke";
	public const string BoletimSoftRow = "SaphBoletimSoftRow";
	public const string StatusPillStroke = "SaphStatusPillStroke";
	public const string StatusPrescriptionCard = "SaphStatusPrescriptionCard";
	public const string TextSecondary = "SaphTextSecondary";
	public const string Accent = "SaphAccent";
	public const string OnAccent = "SaphOnAccent";
	public const string OnActionButton = "SaphOnActionButton";
	/// <summary>Fundo dos cartões CHAMADO / PRESCRIÇÃO (antes era StaticResource sempre escuro).</summary>
	public const string PrescricaoCardBg = "SaphPrescricaoCardBg";

	// Páginas modais (Boletim APH, Alterar Endereço, ferramentas): antes eram hex fixos escuros.
	public const string ModalSurface = "SaphModalSurface";
	public const string ModalHeaderBg = "SaphModalHeaderBg";
	public const string ModalHeaderText = "SaphModalHeaderText";
	public const string ModalCard = "SaphModalCard";
	public const string ModalField = "SaphModalField";
	public const string ModalFieldText = "SaphModalFieldText";
	public const string ModalPlaceholder = "SaphModalPlaceholder";
	public const string ModalStroke = "SaphModalStroke";
	public const string ModalLabel = "SaphModalLabel";
	public const string ModalFooterBg = "SaphModalFooterBg";
	public const string ModalSectionHeaderBg = "SaphModalSectionHeaderBg";
	public const string RequiredMark = "SaphRequiredMark";

	// Bloco «OUVIR» (gravação press-hold e transcrição em curso).
	public const string RecordingTint = "SaphRecordingTint";
	public const string RecordingText = "SaphRecordingText";
	public const string ProcessingTint = "SaphProcessingTint";
	public const string ProcessingText = "SaphProcessingText";

	// Cena STATUS «Aguardando regulação»: a ilustração tem fundo branco, logo o canvas segue o tema.
	public const string StatusRegulacaoCanvas = "SaphStatusRegulacaoCanvas";
	public const string StatusRegulacaoPill = "SaphStatusRegulacaoPill";
}
