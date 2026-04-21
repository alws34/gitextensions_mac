using Avalonia.Controls;
using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class CompareToBranchDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public CompareToBranchDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        _ = LoadBranchesAsync();
    }

    private async Task LoadBranchesAsync()
    {
        var branches = await Task.Run(() =>
            _module.GetRefs(RefsFilter.Heads).Select(r => r.LocalName).ToList());

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            BranchCombo.ItemsSource = branches;
            if (branches.Count > 0)
            {
                BranchCombo.SelectedIndex = 0;
            }
        });
    }

    private void BranchCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (BranchCombo.SelectedItem is string branch)
        {
            _ = LoadDiffAsync(branch);
        }
    }

    private async Task LoadDiffAsync(string branch)
    {
        string diff = await Task.Run(() =>
            _module.GitExecutable.GetOutput($"diff {branch}...HEAD"));

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            DiffEditor.Text = diff;
        });
    }
}
