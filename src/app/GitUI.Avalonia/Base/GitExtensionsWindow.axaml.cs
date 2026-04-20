using Avalonia.Controls;

namespace GitUI.Avalonia.Base;

/// <summary>
/// Base class for all top-level windows in GitExtensions Mac.
/// Replaces WinForms GitExtensionsForm.
/// </summary>
public partial class GitExtensionsWindow : Window
{
    public IServiceProvider? ServiceProvider { get; init; }

    public IGitUICommandsSource? UICommandsSource { get; set; }

    protected GitExtensionsWindow()
    {
    }
}
