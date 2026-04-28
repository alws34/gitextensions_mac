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
    private List<BranchItem> _allLocalBranchItems = [];
    private List<string> _allTags = [];
    private string _currentBranch = string.Empty;

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
            string currentBranch = await System.Threading.Tasks.Task.Run(
                () => _module.GetCurrentBranchName());
            var local = await System.Threading.Tasks.Task.Run(
                () => _module.GetRefs(RefsFilter.Heads).Select(r => r.LocalName).OrderBy(n => n).ToList());
            var remotes = await System.Threading.Tasks.Task.Run(
                () => _module.GetRefs(RefsFilter.Remotes).Select(r => r.LocalName).OrderBy(n => n).ToList());
            var tags = await System.Threading.Tasks.Task.Run(
                () => _module.GetRefs(RefsFilter.Tags).Select(r => r.LocalName).OrderByDescending(n => n).ToList());
            var stashes = await System.Threading.Tasks.Task.Run(
                () => _module.GetStashes().Select(s => s.Summary).ToList());

            // Working directory status
            var workingDirFiles = await System.Threading.Tasks.Task.Run(() =>
            {
                string status = _module.GitExecutable.GetOutput("status --porcelain");
                return status.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                             .Where(line => line.Length >= 2)
                             .Select(line =>
                             {
                                 char xy = line[0] != ' ' ? line[0] : line[1];
                                 string name = line.Length >= 3 ? line[3..].Trim() : line.Trim();
                                 return new WorkingDirItem(xy, name);
                             })
                             .Where(item => item.Name.Length > 0)
                             .ToList();
            });

            // Submodules
            string submoduleOut = await System.Threading.Tasks.Task.Run(() =>
                _module.GitExecutable.GetOutput("submodule status"));
            var submoduleItems = submoduleOut
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line =>
                {
                    if (line.Length == 0)
                    {
                        return null;
                    }

                    char statusChar = line[0];
                    string[] parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    string name = parts.Length >= 2 ? parts[1] : string.Empty;
                    return string.IsNullOrEmpty(name) ? null : new SubmoduleItem(name, statusChar.ToString());
                })
                .Where(s => s is not null)
                .ToList()!;

            // Worktrees
            string worktreeOut = await System.Threading.Tasks.Task.Run(() =>
                _module.GitExecutable.GetOutput("worktree list --porcelain"));
            var worktreePaths = worktreeOut
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(l => l.StartsWith("worktree ", StringComparison.Ordinal))
                .Select(l => l["worktree ".Length..].Trim())
                .Where(p => !string.Equals(
                    p.TrimEnd('/'),
                    _module.WorkingDir.TrimEnd('/'),
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            var branchItems = local
                .Select(b => new BranchItem(b, b == currentBranch))
                .ToList();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _currentBranch = currentBranch;
                _allLocalBranchItems = branchItems;
                _allTags = tags;
                WorkingDirList.ItemsSource = workingDirFiles;
                WorkingDirExpander.Header = workingDirFiles.Count > 0
                    ? $"Working Directory ({workingDirFiles.Count})"
                    : "Working Directory";
                LocalBranchesList.ItemsSource = branchItems;
                RemotesList.ItemsSource = remotes;
                TagsList.ItemsSource = tags;
                StashesList.ItemsSource = stashes;
                SubmodulesList.ItemsSource = submoduleItems;
                WorktreesList.ItemsSource = worktreePaths;
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => ErrorOccurred?.Invoke(ex.Message));
        }
    }

    private void LocalBranch_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (LocalBranchesList.SelectedItem is BranchItem item)
        {
            CheckoutRequested?.Invoke(item.Name);
        }
    }

    private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        string filter = SearchBox.Text ?? string.Empty;
        LocalBranchesList.ItemsSource = string.IsNullOrWhiteSpace(filter)
            ? _allLocalBranchItems
            : _allLocalBranchItems.Where(b => b.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
        TagsList.ItemsSource = string.IsNullOrWhiteSpace(filter)
            ? _allTags
            : _allTags.Where(t => t.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    // ── Context menu guards ──────────────────────────────────────────────────

    private void LocalBranchMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (LocalBranchesList.SelectedItem is not BranchItem)
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
        if (LocalBranchesList.SelectedItem is BranchItem item)
        {
            CheckoutRequested?.Invoke(item.Name);
        }
    }

    private void LocalBranch_Merge(object? sender, RoutedEventArgs e)
    {
        if (LocalBranchesList.SelectedItem is BranchItem item)
        {
            StatusRequested?.Invoke($"Merging {item.Name}…");
            _ = RunGitAndRefreshAsync($"merge {item.Name}");
        }
    }

    private void LocalBranch_Delete(object? sender, RoutedEventArgs e)
    {
        if (LocalBranchesList.SelectedItem is BranchItem item)
        {
            _ = RunGitAndRefreshAsync($"branch -d {item.Name}");
        }
    }

    private void LocalBranch_Push(object? sender, RoutedEventArgs e)
    {
        if (LocalBranchesList.SelectedItem is BranchItem item)
        {
            _ = RunGitAndRefreshAsync($"push origin {item.Name}");
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

    // ── Branch rename ────────────────────────────────────────────────────────

    private void LocalBranch_Rename(object? sender, RoutedEventArgs e)
    {
        if (LocalBranchesList.SelectedItem is BranchItem item && _module is not null)
        {
            _ = RenameBranchAsync(item.Name);
        }
    }

    private async System.Threading.Tasks.Task RenameBranchAsync(string oldName)
    {
        var dialog = new Dialogs.RenameBranchDialog(_module!);
        var mainWindow = (global::Avalonia.Application.Current?.ApplicationLifetime
            as global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)
            ?.MainWindow;
        if (mainWindow is null)
        {
            return;
        }

        await dialog.ShowDialog<object?>(mainWindow);
        await RefreshAsync();
    }

    // ── Submodule handlers ──────────────────────────────────────────────────

    private void Submodule_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (SubmodulesList.SelectedItem is SubmoduleItem item)
        {
            OpenSubmodule(item.Name);
        }
    }

    private void Submodule_Open(object? sender, RoutedEventArgs e)
    {
        if (SubmodulesList.SelectedItem is SubmoduleItem item)
        {
            OpenSubmodule(item.Name);
        }
    }

    private void OpenSubmodule(string name)
    {
        if (_module is null)
        {
            return;
        }

        string path = System.IO.Path.Combine(_module.WorkingDir, name);
        CheckoutRequested?.Invoke(path);
    }

    private void Submodule_Update(object? sender, RoutedEventArgs e)
    {
        if (SubmodulesList.SelectedItem is SubmoduleItem item)
        {
            _ = UpdateSubmoduleAsync(item.Name);
        }
    }

    private async System.Threading.Tasks.Task UpdateSubmoduleAsync(string name)
    {
        try
        {
            string result = await System.Threading.Tasks.Task.Run(() =>
                _module!.GitExecutable.GetOutput($"submodule update --init -- \"{name}\""));
            StatusRequested?.Invoke(result.Trim());
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex.Message);
        }
    }

    // ── Worktree handlers ───────────────────────────────────────────────────

    private void Worktree_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (WorktreesList.SelectedItem is string path)
        {
            CheckoutRequested?.Invoke(path);
        }
    }

    // ── Stash handlers ──────────────────────────────────────────────────────

    private void Stash_DoubleTapped(object? sender, TappedEventArgs e)
    {
        int idx = StashesList.SelectedIndex;
        if (idx >= 0)
        {
            _ = StashPopAsync(idx);
        }
    }

    private void StashMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        bool hasSelection = StashesList.SelectedIndex >= 0;
        if (sender is ContextMenu menu)
        {
            foreach (var item in menu.Items.OfType<MenuItem>())
            {
                item.IsEnabled = hasSelection;
            }
        }
    }

    private void Stash_Pop(object? sender, RoutedEventArgs e)
    {
        int idx = StashesList.SelectedIndex;
        if (idx >= 0)
        {
            _ = StashPopAsync(idx);
        }
    }

    private void Stash_Apply(object? sender, RoutedEventArgs e)
    {
        int idx = StashesList.SelectedIndex;
        if (idx >= 0)
        {
            _ = StashApplyAsync(idx);
        }
    }

    private void Stash_Drop(object? sender, RoutedEventArgs e)
    {
        int idx = StashesList.SelectedIndex;
        if (idx >= 0)
        {
            _ = StashDropAsync(idx);
        }
    }

    private async System.Threading.Tasks.Task StashPopAsync(int index)
    {
        try
        {
            await System.Threading.Tasks.Task.Run(() =>
                _module!.GitExecutable.GetOutput($"stash pop stash@{{{index}}}"));
            StatusRequested?.Invoke("Stash popped");
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex.Message);
        }
    }

    private async System.Threading.Tasks.Task StashApplyAsync(int index)
    {
        try
        {
            await System.Threading.Tasks.Task.Run(() =>
                _module!.GitExecutable.GetOutput($"stash apply stash@{{{index}}}"));
            StatusRequested?.Invoke("Stash applied");
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex.Message);
        }
    }

    private async System.Threading.Tasks.Task StashDropAsync(int index)
    {
        try
        {
            await System.Threading.Tasks.Task.Run(() =>
                _module!.GitExecutable.GetOutput($"stash drop stash@{{{index}}}"));
            StatusRequested?.Invoke("Stash dropped");
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex.Message);
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

    private sealed record WorkingDirItem(char StatusChar, string Name)
    {
        public string StatusIcon => StatusChar switch
        {
            'M' => "M",
            'A' => "A",
            'D' => "D",
            'R' => "R",
            'C' => "C",
            'U' => "U",
            '?' => "?",
            '!' => "!",
            _ => " ",
        };

        public string StatusColor => StatusChar switch
        {
            'M' => "#FFFF8C00",
            'A' => "#FF32CD32",
            'D' => "#FFFF4444",
            'R' => "#FFAA44FF",
            'U' => "#FFFF0000",
            '?' => "#FF888888",
            _ => "#FF000000",
        };
    }

    private sealed record SubmoduleItem(string Name, string StatusChar)
    {
        public string DisplayName => StatusChar switch
        {
            "-" => $"○ {Name}",
            "+" => $"↑ {Name}",
            "U" => $"⚠ {Name}",
            _ => $"✓ {Name}",
        };

        public string StatusTooltip => StatusChar switch
        {
            "-" => "Not initialized",
            "+" => "Different HEAD than committed",
            "U" => "Merge conflict",
            _ => "In sync",
        };
    }
}

/// <summary>View model for a local branch item in the left panel.</summary>
public sealed record BranchItem(string Name, bool IsCurrent);
