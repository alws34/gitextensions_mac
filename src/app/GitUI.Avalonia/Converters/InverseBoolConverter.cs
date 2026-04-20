using System.Globalization;
using Avalonia.Data.Converters;

namespace GitUI.Avalonia.Converters;

/// <summary>Inverts a bool: true→false, false→true.</summary>
public class InverseBoolConverter : IValueConverter
{
    public static readonly InverseBoolConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is false;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is false;
}
