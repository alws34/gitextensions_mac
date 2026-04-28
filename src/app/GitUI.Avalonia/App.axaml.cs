using System.ComponentModel.Design;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GitCommands;
using GitCommands.Git;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.Dialogs;
using GitUI.Avalonia.Infrastructure;
using Microsoft.VisualStudio.Threading;
using ReactiveUI;

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
            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;
            desktop.Exit += (_, _) => Settings.Save();

            // Wire macOS native menu bar commands (ignored on non-macOS platforms)
            if (NativeMenu.GetMenu(this) is NativeMenu nativeMenu)
            {
                foreach (var item in nativeMenu.Items)
                {
                    if (item is NativeMenuItem menuItem)
                    {
                        if (menuItem.Header == "About Git Extensions")
                        {
                            menuItem.Command = ReactiveCommand.CreateFromTask(async () =>
                            {
                                var win = desktop.MainWindow;
                                if (win is not null)
                                {
                                    await new AboutDialog().ShowDialog<object?>(win);
                                }
                            });
                        }
                        else if (menuItem.Header == "Settings…")
                        {
                            menuItem.Command = ReactiveCommand.Create(() =>
                            {
                                if (desktop.MainWindow is MainWindow mw)
                                {
                                    mw.OpenSettingsFromNativeMenu();
                                }
                            });
                        }
                        else if (menuItem.Header == "Quit Git Extensions")
                        {
                            menuItem.Command = ReactiveCommand.Create(() =>
                                desktop.Shutdown());
                        }
                    }
                }
            }

            // Open repo from command-line arg (useful for debugging and CLI launch)
            var startupPath = desktop.Args?.FirstOrDefault(a => System.IO.Directory.Exists(a));
            if (startupPath is not null)
            {
                mainWindow.Opened += (_, _) => mainWindow.OpenRepository(startupPath);
            }

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                e.SetObserved();
                System.Diagnostics.Debug.WriteLine($"Unobserved task exception: {e.Exception}");
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (desktop.MainWindow is MainWindow mw)
                    {
                        mw.ShowError(e.Exception.GetBaseException().Message);
                    }
                });
            };
        }

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            string msg = (e.ExceptionObject as Exception)?.ToString()
                         ?? e.ExceptionObject?.ToString()
                         ?? "Unknown error";
            Console.Error.WriteLine($"[GitExtensions fatal] {msg}");
        };

        base.OnFrameworkInitializationCompleted();

        // Must be initialized after base call so Avalonia's SynchronizationContext is installed
        GitUI.ThreadHelper.JoinableTaskContext = new JoinableTaskContext();
    }
}
