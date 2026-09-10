using System.Globalization;
using Microsoft.Maui.Controls;

namespace SaphMovel.Converters;

/// <summary>Maps SelectedTabKey == ConverterParameter to Bold for tab labels.</summary>
public sealed class TabKeyToFontAttributesConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.Ordinal)
			? FontAttributes.Bold
			: FontAttributes.None;

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
