using Avalonia.Controls;
using Avalonia.Interactivity;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class GoToCommitDialog : GitExtensionsDialog
{
    public string? CommitRef { get; private set; }

    public GoToCommitDialog()
    {
        InitializeComponent();
    }

    private void Go_Click(object? sender, RoutedEventArgs e)
    {
        CommitRef = CommitBox.Text?.Trim();
        Close(CommitRef);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
}
