using Avalonia;
using Avalonia.Headless;
using GitUI.Avalonia.Converters;

namespace GitUI.Avalonia.Tests;

internal static class AvaloniaTestHost
{
    private const string RefTypeBrushKey = "RefTypeBrush";
    public static void EnsureStarted()
    {
        if (Application.Current is null)
        {
            AppBuilder.Configure<Application>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();
        }

        Application.Current!.Resources[RefTypeBrushKey] = RefTypeBrushConverter.Instance;
    }
}
