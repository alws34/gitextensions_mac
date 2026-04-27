using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using GitExtensions.Extensibility.Git;
using AvaloniaApp = Avalonia.Application;

namespace GitUI.Avalonia.Converters;

public sealed class RefTypeBrushConverter : IValueConverter
{
    public static readonly RefTypeBrushConverter Instance = new();

    public static string GetResourceKey(bool isRemote, bool isTag) =>
        isTag ? "RefBadgeTagBackground" :
        isRemote ? "RefBadgeRemoteBranchBackground" :
        "RefBadgeLocalBranchBackground";

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IGitRef gitRef)
        {
            string key = GetResourceKey(gitRef.IsRemote, gitRef.IsTag);
            if (AvaloniaApp.Current is IResourceHost host &&
                host.TryGetResource(key, null, out object? res) && res is IBrush brush)
            {
                return brush;
            }
        }

        return Brushes.DodgerBlue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
