using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class CherryPickDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public CherryPickDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
    }

    public CherryPickDialog(GitModule module, string commitHash) : this(module)
    {
        CommitHashTextBox.Text = commitHash;
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        _ = CherryPickAsync();
    }

    private async Task CherryPickAsync()
    {
        string hash = CommitHashTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(hash))
        {
            await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = "Please enter a commit hash.");
            return;
        }

        string noCommitFlag = NoCommitCheckBox.IsChecked == true ? "--no-commit " : string.Empty;
        string result = await Task.Run(() => _module.GitExecutable.GetOutput($"cherry-pick {noCommitFlag}{hash}"));
        await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = result.Trim());

        if (!result.Contains("error") && !result.Contains("conflict"))
        {
            Close(true);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
