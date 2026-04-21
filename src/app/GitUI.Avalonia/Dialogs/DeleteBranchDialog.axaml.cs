using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class DeleteBranchDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public DeleteBranchDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var branches = await Task.Run(() => _module.GetRefs(RefsFilter.Heads));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            BranchList.ItemsSource = branches.Select(b => b.Name).ToList();
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
                _module.GitExecutable.GetOutput($"branch {forceFlag} {branch}");
            }
        });

        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
