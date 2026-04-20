using Avalonia.Controls;
using Avalonia.Input;

namespace GitUI.Avalonia.Base;

/// <summary>
/// Base class for all modal dialogs in GitExtensions Mac.
/// Replaces WinForms GitExtensionsDialog.
/// </summary>
public partial class GitExtensionsDialog : Window
{
    protected GitExtensionsDialog()
    {
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close(null);
        }
    }
}
