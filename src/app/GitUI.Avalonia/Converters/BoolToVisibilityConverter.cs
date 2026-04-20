using System.Globalization;
using Avalonia.Data.Converters;

namespace GitUI.Avalonia.Converters;

/// <summary>Maps bool → IsVisible. true=visible, false=collapsed.</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true;
}
