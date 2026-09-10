using System.Globalization;
using Microsoft.Maui.Controls;

namespace SaphMovel.Converters;

public sealed class StringToImageSourceConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not string s || string.IsNullOrWhiteSpace(s))
			return null;
		return ImageSource.FromFile(s.Trim());
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
