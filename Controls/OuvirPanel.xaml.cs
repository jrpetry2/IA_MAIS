namespace SaphMovel.Controls;

/// <summary>Bloco «OUVIR» press-hold + estados de gravação/transcrição, partilhado pelas modais de ditado.</summary>
public partial class OuvirPanel : ContentView
{
	public OuvirPanel()
	{
		InitializeComponent();
	}

	/// <summary>Área a ligar ao toque nativo (pressionar/soltar).</summary>
	public VisualElement PressArea => PressAreaBorder;
}
