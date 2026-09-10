using System.Globalization;
using Microsoft.Maui.Controls;

namespace SaphMovel.Converters;

/// <summary>Cor do ponto ao lado de IXI 2092 na aba STATUS conforme o estado operacional.</summary>
public sealed class StatusSceneToDotBrushConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var scene = value?.ToString() ?? "Lista";
		var color = scene switch
		{
			"Lista" => Microsoft.Maui.Graphics.Color.FromArgb("#4CAF50"),
			"Deslocar" => Microsoft.Maui.Graphics.Color.FromArgb("#E30613"),
			"AguardandoRegulacao" => Microsoft.Maui.Graphics.Color.FromArgb("#1976D2"),
			_ => Microsoft.Maui.Graphics.Color.FromArgb("#1976D2"),
		};
		return new SolidColorBrush(color);
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
