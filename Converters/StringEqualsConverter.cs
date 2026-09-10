using System.Globalization;
using Microsoft.Maui.Controls;

namespace SaphMovel.Converters;

public sealed class StringEqualsConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.Ordinal);

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
