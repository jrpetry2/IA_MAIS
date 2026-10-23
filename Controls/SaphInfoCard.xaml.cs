namespace SaphMovel.Controls;

/// <summary>Cartão caption + valor usado na aba CHAMADO (e pares em grelha).</summary>
public partial class SaphInfoCard : ContentView
{
	public static readonly BindableProperty CaptionProperty =
		BindableProperty.Create(nameof(Caption), typeof(string), typeof(SaphInfoCard), default(string));

	public static readonly BindableProperty ValueProperty =
		BindableProperty.Create(nameof(Value), typeof(string), typeof(SaphInfoCard), default(string));

	public static readonly BindableProperty ValueFontSizeProperty =
		BindableProperty.Create(nameof(ValueFontSize), typeof(double), typeof(SaphInfoCard), 15d);

	public string Caption
	{
		get => (string)GetValue(CaptionProperty);
		set => SetValue(CaptionProperty, value);
	}

	public string Value
	{
		get => (string)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	public double ValueFontSize
	{
		get => (double)GetValue(ValueFontSizeProperty);
		set => SetValue(ValueFontSizeProperty, value);
	}

	public SaphInfoCard()
	{
		InitializeComponent();
	}
}
