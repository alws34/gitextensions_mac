using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class DeleteRemoteBranchDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public DeleteRemoteBranchDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        _ = LoadRemotesAsync();
    }

    private async Task LoadRemotesAsync()
    {
        var remotes = await _module.GetRemotesAsync();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            RemoteComboBox.ItemsSource = remotes.Select(r => r.Name).ToList();
            if (RemoteComboBox.Items.Count > 0)
            {
                RemoteComboBox.SelectedIndex = 0;
            }
        });
    }

    private void Remote_Changed(object? sender, SelectionChangedEventArgs e)
    {
        _ = LoadRemoteBranchesAsync();
    }

    private async Task LoadRemoteBranchesAsync()
    {
        string remote = RemoteComboBox.SelectedItem?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(remote))
        {
            return;
        }

        var refs = await Task.Run(() => _module.GetRefs(RefsFilter.Remotes));
        string prefix = $"{remote}/";
        var remoteBranches = refs.Where(r => r.Name.StartsWith(prefix))
                                 .Select(r => r.Name[prefix.Length..])
                                 .ToList();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            BranchList.ItemsSource = remoteBranches;
        });
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        _ = DeleteAsync();
    }

    private async Task DeleteAsync()
    {
        string remote = RemoteComboBox.SelectedItem?.ToString() ?? string.Empty;
        var selected = BranchList.SelectedItems?.Cast<string>().ToList() ?? [];
        if (string.IsNullOrEmpty(remote) || selected.Count == 0)
        {
            return;
        }

        await Task.Run(() =>
        {
            foreach (string branch in selected)
            {
                _module.GitExecutable.GetOutput($"push {remote} --delete {branch}");
            }
        });

        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
