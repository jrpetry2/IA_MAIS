using System.Globalization;
using Microsoft.Maui.Controls;

namespace SaphMovel.Converters;

/// <summary><c>true</c> → 1, <c>false</c> → 0 (mantém espaço no layout).</summary>
public sealed class BoolToOpacityConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is true ? 1.0 : 0.0;

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
