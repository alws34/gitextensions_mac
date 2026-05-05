using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace GitUI.Avalonia.Converters;

public sealed class RevisionRowTextBrushConverter : IMultiValueConverter
{
    public static readonly RevisionRowTextBrushConverter Instance = new();

    private static readonly IBrush LightNormalBrush = Brushes.Black;
    private static readonly IBrush DarkNormalBrush = Brushes.White;
    private static readonly IBrush LightHashBrush = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
    private static readonly IBrush DarkHashBrush = new SolidColorBrush(Color.FromRgb(0xC8, 0xC8, 0xC8));
    private static readonly IBrush LightNonRelativeBrush = new SolidColorBrush(Color.FromRgb(0x74, 0x74, 0x74));
    private static readonly IBrush DarkNonRelativeBrush = new SolidColorBrush(Color.FromRgb(0xB8, 0xB8, 0xB8));

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isRelative = values.Count < 1 || values[0] is not bool relative || relative;
        bool isSelected = values.Count > 1 && values[1] is bool selected && selected;
        bool isHash = string.Equals(parameter?.ToString(), "hash", StringComparison.OrdinalIgnoreCase);

        return GetBrush(
            isRelative,
            isSelected,
            isHash,
            Application.Current?.ActualThemeVariant);
    }

    public static IBrush GetBrush(bool isRelative, bool isSelected, bool isHash, ThemeVariant? themeVariant)
    {
        if (isSelected)
        {
            return Brushes.White;
        }

        bool isDarkTheme = themeVariant == ThemeVariant.Dark;
        if (!isRelative)
        {
            return isDarkTheme ? DarkNonRelativeBrush : LightNonRelativeBrush;
        }

        if (isHash)
        {
            return isDarkTheme ? DarkHashBrush : LightHashBrush;
        }

        return isDarkTheme ? DarkNormalBrush : LightNormalBrush;
    }
}
