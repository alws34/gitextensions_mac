using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class MergeBranchDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public MergeBranchDialog(GitModule module)
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
            BranchComboBox.ItemsSource = branches.Select(b => b.Name).ToList();
            if (BranchComboBox.Items.Count > 0)
            {
                BranchComboBox.SelectedIndex = 0;
            }
        });
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        _ = MergeAsync();
    }

    private async Task MergeAsync()
    {
        string branch = BranchComboBox.SelectedItem?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(branch))
        {
            return;
        }

        string flag = FastForwardRadio.IsChecked == true ? "--ff-only"
            : NoFastForwardRadio.IsChecked == true ? "--no-ff"
            : "--squash";

        await Task.Run(() => _module.GitExecutable.GetOutput($"merge {flag} {branch}"));
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
