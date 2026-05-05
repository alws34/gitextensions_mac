using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitCommands;
using GitExtensions.Extensibility;

namespace GitUI.Avalonia.LeftPanel;

public partial class RepoBrowserPanel : UserControl
{
    private GitModule? _module;
    private List<BranchItem> _allLocalBranchItems = [];
    private List<string> _allRemotes = [];
    private List<string> _allTags = [];
    private string _currentBranch = string.Empty;

    public event Action<string>? CheckoutRequested;
    public event Action<string>? OpenRepositoryRequested;
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
                _allRemotes = remotes;
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
        RemotesList.ItemsSource = string.IsNullOrWhiteSpace(filter)
            ? _allRemotes
            : _allRemotes.Where(r => r.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
        TagsList.ItemsSource = string.IsNullOrWhiteSpace(filter)
            ? _allTags
            : _allTags.Where(t => t.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private void CollapseAll_Click(object? sender, RoutedEventArgs e)
    {
        WorkingDirExpander.IsExpanded = false;
        LocalBranchesExpander.IsExpanded = false;
        RemotesExpander.IsExpanded = false;
        TagsExpander.IsExpanded = false;
    }

    private void ContextList_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not ListBox listBox || !e.GetCurrentPoint(listBox).Properties.IsRightButtonPressed)
        {
            return;
        }

        if (FindAncestor<ListBoxItem>(e.Source as Visual) is { DataContext: { } item })
        {
            listBox.SelectedItem = item;
        }
    }

    private static T? FindAncestor<T>(Visual? source)
        where T : Visual
    {
        for (Visual? current = source; current is not null; current = current.GetVisualParent())
        {
            if (current is T match)
            {
                return match;
            }
        }

        return null;
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

    private void RemoteMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (RemotesList.SelectedItem is null)
        {
            e.Cancel = true;
        }
    }

    private void SubmoduleMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (SubmodulesList.SelectedItem is null)
        {
            e.Cancel = true;
        }
    }

    private void WorktreeMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (WorktreesList.SelectedItem is null)
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

    private void LocalBranch_CreateFrom(object? sender, RoutedEventArgs e)
    {
        if (_module is null)
        {
            return;
        }

        _ = ShowDialogAndRefreshAsync(new Dialogs.CreateBranchDialog(_module));
    }

    private void LocalBranch_Push(object? sender, RoutedEventArgs e)
    {
        if (LocalBranchesList.SelectedItem is BranchItem item)
        {
            _ = RunGitAndRefreshAsync($"push origin {item.Name}");
        }
    }

    private void LocalBranch_ResetToRemote(object? sender, RoutedEventArgs e)
    {
        if (LocalBranchesList.SelectedItem is BranchItem item)
        {
            _ = RunGitAndRefreshAsync($"reset --hard origin/{item.Name}");
        }
    }

    private void LocalBranch_CopyName(object? sender, RoutedEventArgs e)
    {
        if (LocalBranchesList.SelectedItem is BranchItem item)
        {
            _ = CopyToClipboardAsync(item.Name, "Branch name");
        }
    }

    // ── Remote context menu handlers ─────────────────────────────────────────

    private void Remote_Fetch(object? sender, RoutedEventArgs e)
    {
        if (RemotesList.SelectedItem is string remote)
        {
            string remoteName = GetRemoteName(remote);
            StatusRequested?.Invoke($"Fetching {remoteName}…");
            _ = RunGitAndRefreshAsync($"fetch {remoteName.Quote()}");
        }
    }

    private void Remote_FetchPrune(object? sender, RoutedEventArgs e)
    {
        if (RemotesList.SelectedItem is string remote)
        {
            string remoteName = GetRemoteName(remote);
            StatusRequested?.Invoke($"Fetching and pruning {remoteName}…");
            _ = RunGitAndRefreshAsync($"fetch --prune {remoteName.Quote()}");
        }
    }

    private void Remote_CheckoutLocal(object? sender, RoutedEventArgs e)
    {
        if (RemotesList.SelectedItem is string remote)
        {
            string branchName = GetRemoteBranchName(remote);
            _ = RunGitAndRefreshAsync($"checkout -b {branchName.Quote()} {remote.Quote()}");
        }
    }

    private void Remote_CopyBranchName(object? sender, RoutedEventArgs e)
    {
        if (RemotesList.SelectedItem is string remote)
        {
            _ = CopyToClipboardAsync(remote, "Remote branch name");
        }
    }

    private void Remote_CopyName(object? sender, RoutedEventArgs e)
    {
        if (RemotesList.SelectedItem is string remote)
        {
            _ = CopyToClipboardAsync(GetRemoteName(remote), "Remote name");
        }
    }

    private void Remote_Delete(object? sender, RoutedEventArgs e)
    {
        if (_module is null)
        {
            return;
        }

        _ = ShowDialogAndRefreshAsync(new Dialogs.DeleteRemoteBranchDialog(_module));
    }

    // ── Tag context menu handlers ────────────────────────────────────────────

    private void Tag_CreateBranch(object? sender, RoutedEventArgs e)
    {
        if (_module is null)
        {
            return;
        }

        _ = ShowDialogAndRefreshAsync(new Dialogs.CreateBranchDialog(_module));
    }

    private void Tag_Delete(object? sender, RoutedEventArgs e)
    {
        if (TagsList.SelectedItem is string tag)
        {
            _ = RunGitAndRefreshAsync($"tag -d {tag}");
        }
    }

    private void Tag_CopyName(object? sender, RoutedEventArgs e)
    {
        if (TagsList.SelectedItem is string tag)
        {
            _ = CopyToClipboardAsync(tag, "Tag name");
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

    private void Submodule_Reveal(object? sender, RoutedEventArgs e)
    {
        if (SubmodulesList.SelectedItem is SubmoduleItem item)
        {
            RevealPath(GetSubmodulePath(item.Name));
        }
    }

    private void Submodule_CopyName(object? sender, RoutedEventArgs e)
    {
        if (SubmodulesList.SelectedItem is SubmoduleItem item)
        {
            _ = CopyToClipboardAsync(item.Name, "Submodule name");
        }
    }

    private void Submodule_CopyPath(object? sender, RoutedEventArgs e)
    {
        if (SubmodulesList.SelectedItem is SubmoduleItem item)
        {
            _ = CopyToClipboardAsync(GetSubmodulePath(item.Name), "Submodule path");
        }
    }

    private void Submodule_Manage(object? sender, RoutedEventArgs e)
    {
        if (_module is not null)
        {
            _ = ShowDialogAndRefreshAsync(new Dialogs.SubmodulesDialog(_module));
        }
    }

    private void OpenSubmodule(string name)
    {
        if (_module is null)
        {
            return;
        }

        OpenRepositoryRequested?.Invoke(GetSubmodulePath(name));
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
            OpenWorktree(path);
        }
    }

    private void Worktree_Open(object? sender, RoutedEventArgs e)
    {
        if (WorktreesList.SelectedItem is string path)
        {
            OpenWorktree(path);
        }
    }

    private void Worktree_Reveal(object? sender, RoutedEventArgs e)
    {
        if (WorktreesList.SelectedItem is string path)
        {
            RevealPath(path);
        }
    }

    private void Worktree_CopyPath(object? sender, RoutedEventArgs e)
    {
        if (WorktreesList.SelectedItem is string path)
        {
            _ = CopyToClipboardAsync(path, "Worktree path");
        }
    }

    private void Worktree_Manage(object? sender, RoutedEventArgs e)
    {
        if (_module is not null)
        {
            _ = ShowDialogAndRefreshAsync(new Dialogs.ManageWorktreeDialog(_module));
        }
    }

    private void OpenWorktree(string path) => OpenRepositoryRequested?.Invoke(path);

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
                if (string.Equals(item.Tag?.ToString(), "RequiresSelection", StringComparison.Ordinal))
                {
                    item.IsEnabled = hasSelection;
                }
            }
        }
    }

    private void Stash_Manage(object? sender, RoutedEventArgs e)
    {
        if (_module is not null)
        {
            _ = ShowDialogAndRefreshAsync(new Dialogs.StashDialog(_module));
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

    private void Stash_CopyText(object? sender, RoutedEventArgs e)
    {
        if (StashesList.SelectedItem is string stash)
        {
            _ = CopyToClipboardAsync(stash, "Stash text");
        }
    }

    private void Stash_CopyRef(object? sender, RoutedEventArgs e)
    {
        int idx = StashesList.SelectedIndex;
        if (idx >= 0)
        {
            _ = CopyToClipboardAsync(GetStashRef(idx), "Stash ref");
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

    private async System.Threading.Tasks.Task ShowDialogAndRefreshAsync(global::Avalonia.Controls.Window dialog)
    {
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

    private async System.Threading.Tasks.Task CopyToClipboardAsync(string text, string description)
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is null)
            {
                ErrorOccurred?.Invoke("Clipboard is not available.");
                return;
            }

            await clipboard.SetTextAsync(text);
            StatusRequested?.Invoke($"{description} copied");
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex.Message);
        }
    }

    private void RevealPath(string path)
    {
        try
        {
            string fullPath = System.IO.Path.GetFullPath(path);
            if (!System.IO.Directory.Exists(fullPath) && !System.IO.File.Exists(fullPath))
            {
                ErrorOccurred?.Invoke($"Path does not exist: {fullPath}");
                return;
            }

            System.Diagnostics.Process.Start(CreateRevealPathProcessStartInfo(fullPath));
            StatusRequested?.Invoke($"Revealing {fullPath}");
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex.Message);
        }
    }

    private static System.Diagnostics.ProcessStartInfo CreateRevealPathProcessStartInfo(string path)
    {
        if (OperatingSystem.IsMacOS())
        {
            var psi = new System.Diagnostics.ProcessStartInfo { FileName = "open", UseShellExecute = false };
            psi.ArgumentList.Add("-R");
            psi.ArgumentList.Add(path);
            return psi;
        }

        if (OperatingSystem.IsWindows())
        {
            var psi = new System.Diagnostics.ProcessStartInfo { FileName = "explorer.exe", UseShellExecute = false };
            psi.ArgumentList.Add($"/select,\"{path}\"");
            return psi;
        }

        string folder = System.IO.Directory.Exists(path)
            ? path
            : System.IO.Path.GetDirectoryName(path) ?? path;
        var fallback = new System.Diagnostics.ProcessStartInfo { FileName = "xdg-open", UseShellExecute = false };
        fallback.ArgumentList.Add(folder);
        return fallback;
    }

    private string GetSubmodulePath(string name)
    {
        if (_module is null)
        {
            return name;
        }

        return System.IO.Path.GetFullPath(System.IO.Path.Combine(_module.WorkingDir, name));
    }

    private static string GetRemoteName(string remoteBranch)
    {
        int slashIndex = remoteBranch.IndexOf('/');
        return slashIndex > 0 ? remoteBranch[..slashIndex] : remoteBranch;
    }

    private static string GetRemoteBranchName(string remoteBranch)
    {
        int slashIndex = remoteBranch.IndexOf('/');
        return slashIndex >= 0 && slashIndex + 1 < remoteBranch.Length
            ? remoteBranch[(slashIndex + 1)..]
            : remoteBranch;
    }

    private static string GetStashRef(int index) => $"stash@{{{index}}}";

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
public sealed record BranchItem(string Name, bool IsCurrent)
{
    public int Depth => Math.Max(0, Name.Count(c => c == '/'));

    public string DisplayName => Name.Contains('/', StringComparison.Ordinal)
        ? Name[(Name.LastIndexOf('/') + 1)..]
        : Name;

    public string Icon => IsCurrent ? "●" : "○";

    public string IconColor => IsCurrent ? "#FF2E7D32" : "#FF888888";

    public string AheadBehind => string.Empty;

    public bool HasAheadBehind => false;
}
