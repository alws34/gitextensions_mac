using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility;

namespace GitUI.Avalonia.LeftPanel;

public partial class RepoBrowserPanel : UserControl
{
    private GitModule? _module;
    private List<string> _allLocalBranches = [];
    private List<string> _allTags = [];

    public event Action<string>? CheckoutRequested;
    public event Action<string>? StatusRequested;
    public event Action<string>? ErrorOccurred;

    public RepoBrowserPanel() => InitializeComponent();

    public void SetModule(GitModule module)
    {
        _module = module;
        _ = RefreshAsync();
    }

    public async System.Threading.Tasks.Task RefreshAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            var local = await System.Threading.Tasks.Task.Run(
                () => _module.GetRefs(RefsFilter.Heads).Select(r => r.LocalName).OrderBy(n => n).ToList());
            var remotes = await System.Threading.Tasks.Task.Run(
                () => _module.GetRefs(RefsFilter.Remotes).Select(r => r.LocalName).OrderBy(n => n).ToList());
            var tags = await System.Threading.Tasks.Task.Run(
                () => _module.GetRefs(RefsFilter.Tags).Select(r => r.LocalName).OrderByDescending(n => n).ToList());
            var stashes = await System.Threading.Tasks.Task.Run(
                () => _module.GetStashes().Select(s => s.Summary).ToList());

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _allLocalBranches = local;
                _allTags = tags;
                LocalBranchesList.ItemsSource = local;
                RemotesList.ItemsSource = remotes;
                TagsList.ItemsSource = tags;
                StashesList.ItemsSource = stashes;
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => ErrorOccurred?.Invoke(ex.Message));
        }
    }

    private void LocalBranch_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (LocalBranchesList.SelectedItem is string branch)
        {
            CheckoutRequested?.Invoke(branch);
        }
    }

    private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        string filter = SearchBox.Text ?? string.Empty;
        LocalBranchesList.ItemsSource = string.IsNullOrWhiteSpace(filter)
            ? _allLocalBranches
            : _allLocalBranches.Where(b => b.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
        TagsList.ItemsSource = string.IsNullOrWhiteSpace(filter)
            ? _allTags
            : _allTags.Where(t => t.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    // ── Context menu guards ──────────────────────────────────────────────────

    private void LocalBranchMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (LocalBranchesList.SelectedItem is null)
        {
            e.Cancel = true;
        }
    }

    private void TagMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (TagsList.SelectedItem is null)
        {
            e.Cancel = true;
        }
    }

    // ── Branch context menu handlers ─────────────────────────────────────────

    private void LocalBranch_Checkout(object? sender, RoutedEventArgs e)
    {
        if (LocalBranchesList.SelectedItem is string branch)
        {
            CheckoutRequested?.Invoke(branch);
        }
    }

    private void LocalBranch_Merge(object? sender, RoutedEventArgs e)
    {
        if (LocalBranchesList.SelectedItem is string branch)
        {
            StatusRequested?.Invoke($"Merging {branch}…");
            _ = RunGitAndRefreshAsync($"merge {branch}");
        }
    }

    private void LocalBranch_Delete(object? sender, RoutedEventArgs e)
    {
        if (LocalBranchesList.SelectedItem is string branch)
        {
            _ = RunGitAndRefreshAsync($"branch -d {branch}");
        }
    }

    private void LocalBranch_Push(object? sender, RoutedEventArgs e)
    {
        if (LocalBranchesList.SelectedItem is string branch)
        {
            _ = RunGitAndRefreshAsync($"push origin {branch}");
        }
    }

    // ── Tag context menu handlers ────────────────────────────────────────────

    private void Tag_Delete(object? sender, RoutedEventArgs e)
    {
        if (TagsList.SelectedItem is string tag)
        {
            _ = RunGitAndRefreshAsync($"tag -d {tag}");
        }
    }

    // ── Shared helpers ───────────────────────────────────────────────────────

    private async System.Threading.Tasks.Task RunGitAndRefreshAsync(string args)
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.GitExecutable.GetOutputAsync(args);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => ErrorOccurred?.Invoke(ex.Message));
        }
    }
}
