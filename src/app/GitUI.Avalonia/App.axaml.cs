using System.ComponentModel.Design;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GitCommands;
using GitCommands.Git;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.Infrastructure;

namespace GitUI.Avalonia;

public partial class App : Application
{
    public static ISettingsBackend Settings { get; private set; } = null!;
    public static ICredentialStore Credentials { get; private set; } = null!;
    public static IGitExecutorProvider GitExecutorProvider { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        Settings = new JsonSettingsBackend();

        if (OperatingSystem.IsMacOS())
        {
            Credentials = new MacKeychainCredentialStore();
        }

        var serviceContainer = new ServiceContainer();
        serviceContainer.AddService<IGitDirectoryResolver>(new GitDirectoryResolver());
        GitCommands.ServiceContainerRegistry.RegisterServices(serviceContainer);
        GitExecutorProvider = serviceContainer.GetRequiredService<IGitExecutorProvider>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            desktop.Exit += (_, _) => Settings.Save();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
