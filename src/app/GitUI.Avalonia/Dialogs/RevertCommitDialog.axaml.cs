using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class RevertCommitDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private readonly string _commitHash;

    public RevertCommitDialog(GitModule module, string commitHash)
    {
        _module = module;
        _commitHash = commitHash;
        InitializeComponent();
        _ = LoadCommitInfoAsync();
    }

    private async Task LoadCommitInfoAsync()
    {
        string info = await Task.Run(() =>
            _module.GitExecutable.GetOutput($"log -1 --format=%h %s {_commitHash}").Trim());
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            CommitInfoLabel.Text = $"Revert: {info}";
        });
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        _ = RevertAsync();
    }

    private async Task RevertAsync()
    {
        string noCommitFlag = NoCommitCheckBox.IsChecked == true ? "--no-commit " : string.Empty;
        string result = await Task.Run(() => _module.GitExecutable.GetOutput($"revert {noCommitFlag}{_commitHash}"));
        await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = result.Trim());

        if (!result.Contains("error") && !result.Contains("conflict"))
        {
            Close(true);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
