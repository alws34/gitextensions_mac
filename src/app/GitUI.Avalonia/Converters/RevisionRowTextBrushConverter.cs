using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace GitUI.Avalonia.Converters;

public sealed class RevisionRowTextBrushConverter : IMultiValueConverter
{
    public static readonly RevisionRowTextBrushConverter Instance = new();

    private static readonly IBrush NormalBrush = new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20));
    private static readonly IBrush HashBrush = new SolidColorBrush(Color.FromRgb(0x68, 0x68, 0x68));
    private static readonly IBrush NonRelativeBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x8A, 0x8A));

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isRelative = values.Count < 1 || values[0] is not bool relative || relative;
        bool isSelected = values.Count > 1 && values[1] is bool selected && selected;
        if (isSelected)
        {
            return Brushes.White;
        }

        if (!isRelative)
        {
            return NonRelativeBrush;
        }

        return string.Equals(parameter?.ToString(), "hash", StringComparison.OrdinalIgnoreCase)
            ? HashBrush
            : NormalBrush;
    }
}
