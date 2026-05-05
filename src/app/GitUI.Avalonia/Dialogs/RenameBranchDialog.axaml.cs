using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtUtils;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class RenameBranchDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private readonly string? _defaultBranch;

    public RenameBranchDialog(GitModule module, string? defaultBranch = null)
    {
        _module = module;
        _defaultBranch = defaultBranch;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var branches = await Task.Run(() => _module.GetRefs(RefsFilter.Heads));
        string currentBranch = await Task.Run(() => _module.GitExecutable.GetOutput("branch --show-current").Trim());
        var branchNames = branches.Select(b => b.Name).ToList();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            OldNameComboBox.ItemsSource = branchNames;
            string selectedBranch = string.IsNullOrWhiteSpace(_defaultBranch)
                ? currentBranch
                : _defaultBranch;
            int idx = branchNames.FindIndex(branch => string.Equals(branch, selectedBranch, StringComparison.OrdinalIgnoreCase));
            OldNameComboBox.SelectedIndex = idx >= 0 ? idx : 0;
        });
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        _ = RenameAsync();
    }

    private async Task RenameAsync()
    {
        string oldName = OldNameComboBox.SelectedItem?.ToString() ?? string.Empty;
        string newName = NewNameTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName))
        {
            return;
        }

        await Task.Run(() => _module.GitExecutable.GetOutput($"branch -m {oldName.Quote()} {newName.Quote()}"));
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
