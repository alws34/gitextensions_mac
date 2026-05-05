using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtUtils;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class DeleteBranchDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private readonly string? _defaultBranch;

    public DeleteBranchDialog(GitModule module, string? defaultBranch = null)
    {
        _module = module;
        _defaultBranch = defaultBranch;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var branches = await Task.Run(() => _module.GetRefs(RefsFilter.Heads));
        var branchNames = branches.Select(b => b.Name).ToList();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            BranchList.ItemsSource = branchNames;
            string? selectedBranch = string.IsNullOrWhiteSpace(_defaultBranch)
                ? null
                : branchNames.FirstOrDefault(branch => string.Equals(branch, _defaultBranch, StringComparison.OrdinalIgnoreCase));
            if (selectedBranch is not null)
            {
                BranchList.SelectedItems?.Add(selectedBranch);
            }
        });
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        _ = DeleteAsync();
    }

    private async Task DeleteAsync()
    {
        var selected = BranchList.SelectedItems?.Cast<string>().ToList() ?? [];
        if (selected.Count == 0)
        {
            return;
        }

        string forceFlag = ForceCheckBox.IsChecked == true ? "-D" : "-d";
        await Task.Run(() =>
        {
            foreach (string branch in selected)
            {
                _module.GitExecutable.GetOutput($"branch {forceFlag} {branch.Quote()}");
            }
        });

        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
