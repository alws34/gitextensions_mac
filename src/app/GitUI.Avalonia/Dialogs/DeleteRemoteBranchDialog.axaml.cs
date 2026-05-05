using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtUtils;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class DeleteRemoteBranchDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private readonly string? _defaultRemote;
    private readonly string? _defaultBranch;

    public DeleteRemoteBranchDialog(GitModule module, string? defaultRemoteBranch = null)
    {
        _module = module;
        (_defaultRemote, _defaultBranch) = SplitRemoteBranch(defaultRemoteBranch);
        InitializeComponent();
        _ = LoadRemotesAsync();
    }

    private async Task LoadRemotesAsync()
    {
        var remotes = await _module.GetRemotesAsync();
        var remoteNames = remotes.Select(r => r.Name).ToList();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            RemoteComboBox.ItemsSource = remoteNames;
            if (remoteNames.Count == 0)
            {
                return;
            }

            int defaultIndex = string.IsNullOrWhiteSpace(_defaultRemote)
                ? -1
                : remoteNames.FindIndex(remote => string.Equals(remote, _defaultRemote, StringComparison.OrdinalIgnoreCase));
            RemoteComboBox.SelectedIndex = defaultIndex >= 0 ? defaultIndex : 0;
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
            string? selectedBranch = string.IsNullOrWhiteSpace(_defaultBranch)
                ? null
                : remoteBranches.FirstOrDefault(branch => string.Equals(branch, _defaultBranch, StringComparison.OrdinalIgnoreCase));
            if (string.Equals(remote, _defaultRemote, StringComparison.OrdinalIgnoreCase)
                && selectedBranch is not null)
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
                _module.GitExecutable.GetOutput($"push {remote.Quote()} --delete {branch.Quote()}");
            }
        });

        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);

    private static (string? remote, string? branch) SplitRemoteBranch(string? remoteBranch)
    {
        if (string.IsNullOrWhiteSpace(remoteBranch))
        {
            return (null, null);
        }

        int slashIndex = remoteBranch.IndexOf('/');
        return slashIndex > 0 && slashIndex + 1 < remoteBranch.Length
            ? (remoteBranch[..slashIndex], remoteBranch[(slashIndex + 1)..])
            : (null, remoteBranch);
    }
}
