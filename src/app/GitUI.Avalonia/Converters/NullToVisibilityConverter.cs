using System.Globalization;
using Avalonia.Data.Converters;

namespace GitUI.Avalonia.Converters;

/// <summary>Maps null → IsVisible=false, non-null → IsVisible=true.</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public static readonly NullToVisibilityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
