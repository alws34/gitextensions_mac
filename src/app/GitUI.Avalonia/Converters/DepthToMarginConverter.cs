using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace GitUI.Avalonia.Converters;

/// <summary>Converts an integer depth to a left-indent Thickness for branch tree items.</summary>
public class DepthToMarginConverter : IValueConverter
{
    public static readonly DepthToMarginConverter Instance = new();

    private const double IndentWidth = 16.0;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        int depth = value is int d ? d : 0;
        return new Thickness(depth * IndentWidth, 0, 0, 0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
