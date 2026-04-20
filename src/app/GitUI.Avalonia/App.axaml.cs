using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace GitUI.Avalonia;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new global::Avalonia.Controls.Window
            {
                Title = "Git Extensions",
                Width = 1024,
                Height = 768,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
