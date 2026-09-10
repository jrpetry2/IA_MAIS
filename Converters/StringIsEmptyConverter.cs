using System.Globalization;
using Microsoft.Maui.Controls;

namespace SaphMovel.Converters;

public sealed class StringIsEmptyConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		value is not string s || string.IsNullOrWhiteSpace(s);

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
