using Avalonia.Controls;
using Avalonia.Interactivity;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class GoToCommitDialog : GitExtensionsDialog
{
    public string? ResultHash { get; private set; }

    public GoToCommitDialog()
    {
        InitializeComponent();
    }

    private void HashBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        string text = HashBox.Text?.Trim() ?? string.Empty;
        bool valid = text.Length >= 4 &&
                     text.All(c => (c >= '0' && c <= '9') ||
                                   (c >= 'a' && c <= 'f') ||
                                   (c >= 'A' && c <= 'F'));
        GoButton.IsEnabled = valid;
        ErrorLabel.IsVisible = text.Length > 0 && !valid;
        ErrorLabel.Text = "Enter a valid hex hash (at least 4 characters)";
    }

    private void Go_Click(object? sender, RoutedEventArgs e)
    {
        ResultHash = HashBox.Text?.Trim();
        Close(ResultHash);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
}
