using System.Globalization;
using Microsoft.Maui.Controls;

namespace SaphMovel.Converters;

public sealed class InvertedBoolConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var b = value is true;
		return !b;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
