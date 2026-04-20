using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GitUI.Avalonia.Infrastructure;

namespace GitUI.Avalonia;

public partial class App : Application
{
    public static ISettingsBackend Settings { get; private set; } = null!;
    public static ICredentialStore Credentials { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        Settings = new JsonSettingsBackend();
        if (OperatingSystem.IsMacOS())
        {
            Credentials = new MacKeychainCredentialStore();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new global::Avalonia.Controls.Window
            {
                Title = "Git Extensions",
                Width = 1024,
                Height = 768,
            };
            desktop.Exit += (_, _) => Settings.Save();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
