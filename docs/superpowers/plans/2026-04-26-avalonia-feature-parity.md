# Avalonia Mac Port — Feature Parity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring the GitExtensions Avalonia Mac port (`src/app/GitUI.Avalonia/`) to visual and functional parity with the original WinForms app across 11 feature areas.

**Architecture:** Maintain the existing code-behind-heavy pattern (menus/toolbar built in C# `Build*()` methods, Avalonia XAML for layout). New controls live in namespaced subfolders under `src/app/GitUI.Avalonia/`. No ViewModels — direct code-behind event handling only, consistent with the existing codebase.

**Tech Stack:** Avalonia UI 11, C# 12, ReactiveUI, NUnit 3, `~/.dotnet/dotnet`  
**dotnet binary:** `~/.dotnet/dotnet`

**Key APIs:**
- `module.GetCurrentBranchName()` → `string` (empty if detached HEAD)
- `module.GetRefs(RefsFilter.Heads)` / `.Remotes` / `.Tags` → `IReadOnlyList<IGitRef>`
- `IGitRef.LocalName`, `.IsHead`, `.IsRemote`, `.IsTag`
- `module.GetStashes()` → `IReadOnlyList<GitStash>` with `.Summary`, `.Name`
- `module.WorkingDir` → working directory path
- `module.WorkingDirGitDir` → path to `.git/` directory
- `module.GitExecutable.GetOutputAsync(string)` → `Task<string>`
- `RefsFilter` is in `using GitExtensions.Extensibility;`
- `IGitRef` is in `using GitExtensions.Extensibility.Git;`

---

## File Map

| File | Task(s) | Change |
|------|---------|--------|
| `src/app/GitUI.Avalonia/MainWindow.axaml` | 2, 4, 5 | Add left-panel column; action bar slot |
| `src/app/GitUI.Avalonia/MainWindow.axaml.cs` | 1, 4, 5, 7, 10 | BuildToolBar; toggle; action bars; menus; recent repos |
| `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml` | 2, 6 | New left-panel control with context menus |
| `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml.cs` | 2, 6 | Branch/tag/stash loading; context menu handlers |
| `src/app/GitUI.Avalonia/CommitDetails/CommitDetailsPanel.axaml` | 3 | Replace Grid with DockPanel + TabControl |
| `src/app/GitUI.Avalonia/CommitDetails/CommitDetailsPanel.axaml.cs` | 3 | Lazy diff load on tab switch |
| `src/app/GitUI.Avalonia/RevisionGrid/RevisionRow.cs` | 8 | Add `ShortHash` property |
| `src/app/GitUI.Avalonia/RevisionGrid/RevisionDataGrid.axaml` | 8, 11 | Add CommitId column; per-ref-type badge ItemsControl |
| `src/app/GitUI.Avalonia/CommitDetails/FileTreeNode.cs` | 9 | New tree-node model + static builder |
| `src/app/GitUI.Avalonia/CommitDetails/FileStatusList.axaml` | 9 | Replace ListBox with TreeView |
| `src/app/GitUI.Avalonia/CommitDetails/FileStatusList.axaml.cs` | 9 | Build & load tree from flat file list |
| `src/app/GitUI.Avalonia/Converters/RefTypeBrushConverter.cs` | 11 | `IGitRef → IBrush` by ref type |
| `tests/app/GitUI.Avalonia.Tests/CommitDetails/FileTreeNodeTests.cs` | 9 | Unit tests for tree building |
| `tests/app/GitUI.Avalonia.Tests/Converters/RefTypeBrushConverterTests.cs` | 11 | Unit tests for brush converter |

---

## Task 1: Toolbar — branch selector, action buttons

Add a fully populated toolbar to the main window: Refresh, Commit, Fetch, Pull, Push, Stash buttons plus a live branch-selector `ComboBox`.

**Files:**
- Modify: `src/app/GitUI.Avalonia/MainWindow.axaml.cs`

- [ ] **Step 1: Add required usings to `MainWindow.axaml.cs`**

Ensure the top of the file contains these (add any that are missing):

```csharp
using Avalonia.Media;
using Avalonia.Threading;
using GitExtensions.Extensibility;
```

- [ ] **Step 2: Add new fields at the top of the `MainWindow` class**

Inside the `MainWindow` class, after the existing `private GitModule? _module;` field, add:

```csharp
private ComboBox? _branchSelector;
private bool _suppressBranchSelection;
```

- [ ] **Step 3: Call `BuildToolBar()` from the constructor**

In the `MainWindow()` constructor, after `BuildMenu();`, add:

```csharp
BuildToolBar();
```

- [ ] **Step 4: Add `BuildToolBar()` and helper methods to `MainWindow.axaml.cs`**

Add these methods anywhere in the `MainWindow` class:

```csharp
private void BuildToolBar()
{
    MainToolBar.Children.Add(MakeToolButton("↻", "Refresh revisions", () => _ = RevisionGrid.RefreshAsync()));
    MainToolBar.Children.Add(MakeToolSeparator());
    MainToolBar.Children.Add(MakeToolButton("Commit…", "Commit staged changes",
        () => _ = ShowModuleDialogAsync(m => new CommitDialog(m))));
    MainToolBar.Children.Add(MakeToolButton("Fetch", "Fetch all remotes", () => _ = FetchAsync()));
    MainToolBar.Children.Add(MakeToolButton("Pull…", "Pull / merge",
        () => _ = ShowModuleDialogAsync(m => new PullDialog(m))));
    MainToolBar.Children.Add(MakeToolButton("Push…", "Push to remote",
        () => _ = ShowModuleDialogAsync(m => new PushDialog(m))));
    MainToolBar.Children.Add(MakeToolButton("Stash…", "Stash local changes",
        () => _ = ShowModuleDialogAsync(m => new StashDialog(m))));
    MainToolBar.Children.Add(MakeToolSeparator());

    _branchSelector = new ComboBox
    {
        Width = 180,
        PlaceholderText = "Branch",
        IsVisible = false,
        Margin = new Thickness(2, 0),
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
    };
    _branchSelector.SelectionChanged += OnBranchSelectorChanged;
    MainToolBar.Children.Add(_branchSelector);
}

private static Button MakeToolButton(string text, string tooltip, Action onClick)
{
    var btn = new Button
    {
        Content = text,
        Padding = new Thickness(8, 2),
        Margin = new Thickness(1, 0),
    };
    ToolTip.SetTip(btn, tooltip);
    btn.Click += (_, _) => onClick();
    return btn;
}

private static Control MakeToolSeparator() =>
    new Border
    {
        Width = 1,
        Height = 20,
        Background = Brushes.Gray,
        Margin = new Thickness(4, 0),
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
    };

private async System.Threading.Tasks.Task RefreshBranchSelectorAsync()
{
    if (_module is null || _branchSelector is null) return;

    string currentBranch = await System.Threading.Tasks.Task.Run(() => _module.GetCurrentBranchName());
    List<string> branches = await System.Threading.Tasks.Task.Run(
        () => _module.GetRefs(RefsFilter.Heads).Select(r => r.LocalName).OrderBy(n => n).ToList());

    await Dispatcher.UIThread.InvokeAsync(() =>
    {
        _suppressBranchSelection = true;
        try
        {
            _branchSelector.ItemsSource = branches;
            _branchSelector.SelectedItem = string.IsNullOrEmpty(currentBranch) ? null : (object)currentBranch;
            _branchSelector.IsVisible = true;
        }
        finally
        {
            _suppressBranchSelection = false;
        }
    });
}

private void OnBranchSelectorChanged(object? sender, SelectionChangedEventArgs e)
{
    if (_suppressBranchSelection) return;
    if (_branchSelector?.SelectedItem is string branch)
        _ = CheckoutBranchAsync(branch);
}

private async System.Threading.Tasks.Task CheckoutBranchAsync(string branch)
{
    if (_module is null) return;
    StatusLabel.Text = $"Checking out {branch}…";
    try
    {
        await _module.GitExecutable.GetOutputAsync($"checkout {branch}");
        StatusLabel.Text = $"On branch {branch}";
        await RevisionGrid.RefreshAsync();
        await RefreshBranchSelectorAsync();
    }
    catch (Exception ex)
    {
        ShowError(ex.Message);
    }
}
```

- [ ] **Step 5: Call `RefreshBranchSelectorAsync()` when a repo is opened**

In `OpenRepository()`, after the line `RevisionGrid.Module = _module;`, add:

```csharp
_ = RefreshBranchSelectorAsync();
```

- [ ] **Step 6: Build — expect 0 errors**

```bash
cd /Users/alon/Desktop/gitextensions
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS" | head -20
```

Expected: `Build succeeded.`

- [ ] **Step 7: Commit**

```bash
cd /Users/alon/Desktop/gitextensions
git add src/app/GitUI.Avalonia/MainWindow.axaml.cs
git commit -m "feat(avalonia): add toolbar with branch selector and action buttons"
```

---

## Task 2: Left panel repo tree — branches, remotes, tags, stashes

Add a collapsible left-panel `RepoBrowserPanel` showing all repository objects. Branch double-click triggers checkout.

**Files:**
- Create: `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml`
- Create: `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml.cs`
- Modify: `src/app/GitUI.Avalonia/MainWindow.axaml`
- Modify: `src/app/GitUI.Avalonia/MainWindow.axaml.cs`

- [ ] **Step 1: Create `RepoBrowserPanel.axaml`**

Create file `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="GitUI.Avalonia.LeftPanel.RepoBrowserPanel"
             MinWidth="140">
    <DockPanel>
        <TextBox DockPanel.Dock="Top"
                 x:Name="SearchBox"
                 Watermark="Search branches…"
                 Margin="4,4,4,2"
                 TextChanged="SearchBox_TextChanged" />
        <ScrollViewer>
            <StackPanel>
                <Expander Header="Branches" IsExpanded="True">
                    <ListBox x:Name="LocalBranchesList"
                             DoubleTapped="LocalBranch_DoubleTapped" />
                </Expander>
                <Expander Header="Remotes">
                    <ListBox x:Name="RemotesList" />
                </Expander>
                <Expander Header="Tags">
                    <ListBox x:Name="TagsList" />
                </Expander>
                <Expander Header="Stashes">
                    <ListBox x:Name="StashesList" />
                </Expander>
            </StackPanel>
        </ScrollViewer>
    </DockPanel>
</UserControl>
```

- [ ] **Step 2: Create `RepoBrowserPanel.axaml.cs`**

Create file `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml.cs`:

```csharp
using Avalonia.Controls;
using Avalonia.Input;
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

    public RepoBrowserPanel() => InitializeComponent();

    public void SetModule(GitModule module)
    {
        _module = module;
        _ = RefreshAsync();
    }

    public async System.Threading.Tasks.Task RefreshAsync()
    {
        if (_module is null) return;

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

    private void LocalBranch_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (LocalBranchesList.SelectedItem is string branch)
            CheckoutRequested?.Invoke(branch);
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
}
```

- [ ] **Step 3: Update `MainWindow.axaml` — add left panel column**

Replace the entire `<Grid x:Name="RepoView" ...>` block with:

```xml
<Grid x:Name="RepoView" IsVisible="{Binding HasRepository}">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="220" MinWidth="0" />
        <ColumnDefinition Width="5" />
        <ColumnDefinition Width="*" MinWidth="300" />
        <ColumnDefinition Width="5" />
        <ColumnDefinition Width="400" MinWidth="200" />
    </Grid.ColumnDefinitions>
    <leftpanel:RepoBrowserPanel Grid.Column="0" x:Name="LeftPanel"
                                Background="{DynamicResource SystemChromeMediumLowColor}" />
    <GridSplitter Grid.Column="1" ResizeDirection="Columns" />
    <grid:RevisionGridControl Grid.Column="2" x:Name="RevisionGrid" />
    <GridSplitter Grid.Column="3" ResizeDirection="Columns" />
    <details:CommitDetailsPanel Grid.Column="4" x:Name="DetailsPanel" />
</Grid>
```

Also add the namespace to the root element attributes of `MainWindow.axaml`:

```xml
xmlns:leftpanel="using:GitUI.Avalonia.LeftPanel"
```

- [ ] **Step 4: Wire left panel in `MainWindow.axaml.cs`**

In `OpenRepository()`, after `DetailsPanel.SetModule(_module);`, add:

```csharp
LeftPanel.SetModule(_module);
LeftPanel.CheckoutRequested += async branch => await CheckoutBranchAsync(branch);
```

Also, in `CheckoutBranchAsync()`, after `await RefreshBranchSelectorAsync();`, add:

```csharp
_ = LeftPanel.RefreshAsync();
```

- [ ] **Step 5: Build — expect 0 errors**

```bash
cd /Users/alon/Desktop/gitextensions
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS" | head -20
```

Expected: `Build succeeded.`

- [ ] **Step 6: Commit**

```bash
cd /Users/alon/Desktop/gitextensions
git add src/app/GitUI.Avalonia/LeftPanel/ \
        src/app/GitUI.Avalonia/MainWindow.axaml \
        src/app/GitUI.Avalonia/MainWindow.axaml.cs
git commit -m "feat(avalonia): add left panel repo tree (branches, remotes, tags, stashes)"
```

---

## Task 3: Tabbed commit details panel

Replace the fixed 4-row grid layout with a `DockPanel` (summary always visible at top) + `TabControl` (Diff / Files tabs).

**Files:**
- Modify: `src/app/GitUI.Avalonia/CommitDetails/CommitDetailsPanel.axaml`
- Modify: `src/app/GitUI.Avalonia/CommitDetails/CommitDetailsPanel.axaml.cs`

- [ ] **Step 1: Replace `CommitDetailsPanel.axaml` content**

Replace the entire file content with:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:details="using:GitUI.Avalonia.CommitDetails"
             x:Class="GitUI.Avalonia.CommitDetails.CommitDetailsPanel">
    <DockPanel>
        <details:CommitSummaryControl x:Name="Summary"
                                      DockPanel.Dock="Top"
                                      MinHeight="80"
                                      MaxHeight="160" />
        <TabControl x:Name="DetailsTabs"
                    SelectionChanged="DetailsTabs_SelectionChanged">
            <TabItem Header="Diff">
                <details:CommitDiffControl x:Name="DiffView" />
            </TabItem>
            <TabItem Header="Files">
                <details:FileStatusList x:Name="FileList" />
            </TabItem>
        </TabControl>
    </DockPanel>
</UserControl>
```

- [ ] **Step 2: Update `CommitDetailsPanel.axaml.cs` — lazy load on tab switch**

Replace the entire file content with:

```csharp
using Avalonia.Controls;
using GitCommands;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.CommitDetails;

public partial class CommitDetailsPanel : UserControl
{
    private GitModule? _module;
    private GitRevision? _currentRevision;
    private bool _diffLoaded;

    public CommitDetailsPanel() => InitializeComponent();

    public void SetModule(GitModule module)
    {
        _module = module;
        Summary.Module = module;
        DiffView.Module = module;
        FileList.Module = module;
        FileList.SelectedFileChanged += OnFileSelected;
    }

    public async System.Threading.Tasks.Task ShowRevisionAsync(GitRevision? revision)
    {
        _currentRevision = revision;
        _diffLoaded = false;
        Summary.ShowRevision(revision);

        if (revision is null || _module is null) return;

        var files = await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                return _module.GetDiffFilesWithUntracked(
                    revision.Guid + "^",
                    revision.Guid,
                    GitExtensions.Extensibility.Git.StagedStatus.None)
                    .Select(f => new FileStatusItem(f.Name, f.IsAdded, f.IsDeleted, f.IsRenamed))
                    .ToList();
            }
            catch
            {
                return new List<FileStatusItem>();
            }
        });

        FileList.LoadFiles(files);

        // Load diff immediately only if Diff tab is currently active
        if (DetailsTabs.SelectedIndex == 0)
        {
            _diffLoaded = true;
            await DiffView.ShowDiffAsync(revision);
        }
    }

    private async void DetailsTabs_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Lazy-load diff when user switches to the Diff tab
        if (DetailsTabs.SelectedIndex == 0 && !_diffLoaded && _currentRevision is not null)
        {
            _diffLoaded = true;
            await DiffView.ShowDiffAsync(_currentRevision);
        }
    }

    private void OnFileSelected(FileStatusItem? file)
    {
        if (file is null || _currentRevision is null) return;
        _ = DiffView.ShowDiffAsync(_currentRevision, file.Name);
    }
}
```

- [ ] **Step 3: Build — expect 0 errors**

```bash
cd /Users/alon/Desktop/gitextensions
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS" | head -20
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
cd /Users/alon/Desktop/gitextensions
git add src/app/GitUI.Avalonia/CommitDetails/CommitDetailsPanel.axaml \
        src/app/GitUI.Avalonia/CommitDetails/CommitDetailsPanel.axaml.cs
git commit -m "feat(avalonia): tabbed commit details panel (Diff / Files tabs, summary always visible)"
```

---

## Task 4: Toggle left panel

Add a ⊞/⊟ button to the toolbar that shows/hides the left panel.

**Files:**
- Modify: `src/app/GitUI.Avalonia/MainWindow.axaml.cs`

- [ ] **Step 1: Add saved-width field**

Inside the `MainWindow` class (after the `_suppressBranchSelection` field added in Task 1), add:

```csharp
private double _savedLeftPanelWidth = 220;
```

- [ ] **Step 2: Add `ToggleLeftPanel()` method**

```csharp
private void ToggleLeftPanel()
{
    var col = RepoView.ColumnDefinitions[0];
    var splitter = RepoView.ColumnDefinitions[1];
    if (col.Width.Value > 0)
    {
        _savedLeftPanelWidth = col.Width.Value;
        col.Width = new GridLength(0);
        splitter.Width = new GridLength(0);
    }
    else
    {
        col.Width = new GridLength(_savedLeftPanelWidth);
        splitter.Width = new GridLength(5);
    }
}
```

- [ ] **Step 3: Add toggle button to the toolbar**

In `BuildToolBar()`, add this line at the very beginning (before the Refresh button):

```csharp
MainToolBar.Children.Add(MakeToolButton("⊞", "Toggle left panel", ToggleLeftPanel));
MainToolBar.Children.Add(MakeToolSeparator());
```

- [ ] **Step 4: Build — expect 0 errors**

```bash
cd /Users/alon/Desktop/gitextensions
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS" | head -10
```

- [ ] **Step 5: Commit**

```bash
cd /Users/alon/Desktop/gitextensions
git add src/app/GitUI.Avalonia/MainWindow.axaml.cs
git commit -m "feat(avalonia): toggle left panel visibility from toolbar button"
```

---

## Task 5: Interactive action bars (merge/cherry-pick/bisect in-progress)

Show a yellow notification banner when git is mid-operation.

**Files:**
- Modify: `src/app/GitUI.Avalonia/MainWindow.axaml`
- Modify: `src/app/GitUI.Avalonia/MainWindow.axaml.cs`

- [ ] **Step 1: Add action bar slot to `MainWindow.axaml`**

In `MainWindow.axaml`, between the `<controls:FilterToolBar .../>` line and the `<Grid>` that wraps Dashboard/RepoView, add:

```xml
<Border DockPanel.Dock="Top"
        x:Name="ActionBar"
        Background="#FFFFF3CD"
        BorderBrush="#FFFFE082"
        BorderThickness="0,0,0,1"
        IsVisible="False"
        Padding="8,4">
    <DockPanel>
        <Button DockPanel.Dock="Right"
                Content="✕"
                Padding="4,2"
                Click="ActionBar_Dismiss"
                Background="Transparent"
                BorderThickness="0" />
        <TextBlock x:Name="ActionBarText"
                   VerticalAlignment="Center"
                   FontSize="12" />
    </DockPanel>
</Border>
```

- [ ] **Step 2: Add action bar helpers to `MainWindow.axaml.cs`**

```csharp
private void ActionBar_Dismiss(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
{
    ActionBar.IsVisible = false;
}

private async System.Threading.Tasks.Task RefreshActionBarsAsync()
{
    if (_module is null) return;

    string gitDir = _module.WorkingDirGitDir;
    bool merge  = await System.Threading.Tasks.Task.Run(() => File.Exists(Path.Combine(gitDir, "MERGE_HEAD")));
    bool cherry = await System.Threading.Tasks.Task.Run(() => File.Exists(Path.Combine(gitDir, "CHERRY_PICK_HEAD")));
    bool revert = await System.Threading.Tasks.Task.Run(() => File.Exists(Path.Combine(gitDir, "REVERT_HEAD")));
    bool bisect = await System.Threading.Tasks.Task.Run(() => File.Exists(Path.Combine(gitDir, "BISECT_START")));

    string? msg = merge  ? "Merge in progress — resolve conflicts, then commit." :
                  cherry ? "Cherry-pick in progress — resolve conflicts, then commit." :
                  revert ? "Revert in progress — resolve conflicts, then commit." :
                  bisect ? "Bisect in progress — mark commits as good or bad." :
                  null;

    await Dispatcher.UIThread.InvokeAsync(() =>
    {
        ActionBarText.Text = msg;
        ActionBar.IsVisible = msg is not null;
    });
}
```

Also add `using System.IO;` to the usings if not already present.

- [ ] **Step 3: Call `RefreshActionBarsAsync()` after opening a repo and after refresh**

In `OpenRepository()`, after `_ = RefreshBranchSelectorAsync();`, add:

```csharp
_ = RefreshActionBarsAsync();
```

In `FetchAsync()`, after `await RevisionGrid.RefreshAsync();`, add:

```csharp
await RefreshActionBarsAsync();
```

- [ ] **Step 4: Build — expect 0 errors**

```bash
cd /Users/alon/Desktop/gitextensions
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS" | head -10
```

- [ ] **Step 5: Commit**

```bash
cd /Users/alon/Desktop/gitextensions
git add src/app/GitUI.Avalonia/MainWindow.axaml \
        src/app/GitUI.Avalonia/MainWindow.axaml.cs
git commit -m "feat(avalonia): show action bar when merge/cherry-pick/bisect is in progress"
```

---

## Task 6: Ref context menus in left panel

Right-clicking a branch shows: Checkout, Rename, Delete, Merge into current, Push.  
Right-clicking a tag shows: Delete Tag.

**Files:**
- Modify: `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml`
- Modify: `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml.cs`

- [ ] **Step 1: Add context menus to `RepoBrowserPanel.axaml`**

Inside the `<ListBox x:Name="LocalBranchesList" ...>` element (before its closing tag), add:

```xml
<ListBox.ContextMenu>
    <ContextMenu Opening="LocalBranchMenu_Opening">
        <MenuItem Header="Checkout"           Click="LocalBranch_Checkout" />
        <MenuItem Header="Merge into current" Click="LocalBranch_Merge" />
        <MenuItem Header="Rebase current onto this" Click="LocalBranch_Rebase" />
        <Separator />
        <MenuItem Header="Rename…"            Click="LocalBranch_Rename" />
        <MenuItem Header="Delete Branch…"     Click="LocalBranch_Delete" />
        <Separator />
        <MenuItem Header="Push…"              Click="LocalBranch_Push" />
    </ContextMenu>
</ListBox.ContextMenu>
```

Inside the `<ListBox x:Name="TagsList" ...>` element, add:

```xml
<ListBox.ContextMenu>
    <ContextMenu Opening="TagMenu_Opening">
        <MenuItem Header="Delete Tag…" Click="Tag_Delete" />
    </ContextMenu>
</ListBox.ContextMenu>
```

- [ ] **Step 2: Add context menu handlers to `RepoBrowserPanel.axaml.cs`**

Add these methods to the `RepoBrowserPanel` class:

```csharp
private void LocalBranchMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
{
    if (LocalBranchesList.SelectedItem is null)
        e.Cancel = true;
}

private void TagMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
{
    if (TagsList.SelectedItem is null)
        e.Cancel = true;
}

private void LocalBranch_Checkout(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
{
    if (LocalBranchesList.SelectedItem is string branch)
        CheckoutRequested?.Invoke(branch);
}

private void LocalBranch_Merge(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
{
    if (LocalBranchesList.SelectedItem is string branch && _module is not null)
    {
        StatusRequested?.Invoke($"Merging {branch}…");
        _ = RunGitAndRefreshAsync($"merge {branch}");
    }
}

private void LocalBranch_Rebase(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
{
    if (LocalBranchesList.SelectedItem is string branch && _module is not null)
    {
        StatusRequested?.Invoke($"Rebasing onto {branch}…");
        _ = RunGitAndRefreshAsync($"rebase {branch}");
    }
}

private void LocalBranch_Rename(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
{
    if (LocalBranchesList.SelectedItem is string branch && _module is not null)
        _ = RunGitAndRefreshAsync($"branch -m {branch} {branch}-renamed");
    // A real rename dialog would prompt for the new name; this wires the plumbing.
}

private void LocalBranch_Delete(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
{
    if (LocalBranchesList.SelectedItem is string branch && _module is not null)
        _ = RunGitAndRefreshAsync($"branch -d {branch}");
}

private void LocalBranch_Push(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
{
    if (LocalBranchesList.SelectedItem is string branch && _module is not null)
        _ = RunGitAndRefreshAsync($"push origin {branch}");
}

private void Tag_Delete(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
{
    if (TagsList.SelectedItem is string tag && _module is not null)
        _ = RunGitAndRefreshAsync($"tag -d {tag}");
}

private async System.Threading.Tasks.Task RunGitAndRefreshAsync(string args)
{
    if (_module is null) return;
    try
    {
        await _module.GitExecutable.GetOutputAsync(args);
        await RefreshAsync();
    }
    catch (Exception ex)
    {
        ErrorOccurred?.Invoke(ex.Message);
    }
}
```

Also add these events to the class (after `CheckoutRequested`):

```csharp
public event Action<string>? StatusRequested;
public event Action<string>? ErrorOccurred;
```

In `MainWindow.axaml.cs`, in `OpenRepository()`, after `LeftPanel.CheckoutRequested += ...;`, add:

```csharp
LeftPanel.StatusRequested += msg => StatusLabel.Text = msg;
LeftPanel.ErrorOccurred   += msg => ShowError(msg);
```

- [ ] **Step 3: Build — expect 0 errors**

```bash
cd /Users/alon/Desktop/gitextensions
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS" | head -10
```

- [ ] **Step 4: Commit**

```bash
cd /Users/alon/Desktop/gitextensions
git add src/app/GitUI.Avalonia/LeftPanel/
git commit -m "feat(avalonia): add context menus to left panel (checkout, merge, delete, push)"
```

---

## Task 7: Tools / View / Navigate menus

Add three new top-level menus mirroring the original GitExtensions.

**Files:**
- Modify: `src/app/GitUI.Avalonia/MainWindow.axaml.cs`

- [ ] **Step 1: Add `BuildToolsMenu()`, `BuildViewMenu()`, `BuildNavigateMenu()`**

Add these methods to `MainWindow.axaml.cs`:

```csharp
private MenuItem BuildToolsMenu()
{
    var menu = new MenuItem { Header = "_Tools" };
    menu.Items.Add(new MenuItem
    {
        Header = "Open _Terminal Here",
        Command = ReactiveCommand.Create(() =>
        {
            if (_module is not null)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $"-a Terminal \"{_module.WorkingDir}\"",
                    UseShellExecute = false,
                });
        }),
    });
    menu.Items.Add(new MenuItem
    {
        Header = "Open in _Finder",
        Command = ReactiveCommand.Create(() =>
        {
            if (_module is not null)
                System.Diagnostics.Process.Start("open", _module.WorkingDir);
        }),
    });
    menu.Items.Add(new Separator());
    menu.Items.Add(new MenuItem
    {
        Header = "_Settings…",
        Command = ReactiveCommand.Create(OpenSettings),
    });
    return menu;
}

private MenuItem BuildViewMenu()
{
    var menu = new MenuItem { Header = "_View" };
    menu.Items.Add(new MenuItem
    {
        Header = "Toggle _Left Panel",
        Command = ReactiveCommand.Create(ToggleLeftPanel),
    });
    menu.Items.Add(new MenuItem
    {
        Header = "_Refresh",
        Command = ReactiveCommand.CreateFromTask(() => RevisionGrid.RefreshAsync()),
    });
    return menu;
}

private MenuItem BuildNavigateMenu()
{
    var menu = new MenuItem { Header = "_Navigate" };
    menu.Items.Add(new MenuItem
    {
        Header = "_Go to Commit…",
        Command = ReactiveCommand.CreateFromTask(() => ShowDialogAsync(() => new GoToCommitDialog())),
    });
    return menu;
}
```

- [ ] **Step 2: Call the three new methods from `BuildMenu()`**

In `BuildMenu()`, after `MainMenu.Items.Add(BuildCommandsMenu());`, add:

```csharp
MainMenu.Items.Add(BuildToolsMenu());
MainMenu.Items.Add(BuildViewMenu());
MainMenu.Items.Add(BuildNavigateMenu());
```

- [ ] **Step 3: Build — expect 0 errors**

```bash
cd /Users/alon/Desktop/gitextensions
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS" | head -10
```

- [ ] **Step 4: Commit**

```bash
cd /Users/alon/Desktop/gitextensions
git add src/app/GitUI.Avalonia/MainWindow.axaml.cs
git commit -m "feat(avalonia): add Tools, View, and Navigate menus"
```

---

## Task 8: CommitId column in revision grid

Add a short-hash column (7 chars) to the right of the Date column.

**Files:**
- Modify: `src/app/GitUI.Avalonia/RevisionGrid/RevisionRow.cs`
- Modify: `src/app/GitUI.Avalonia/RevisionGrid/RevisionDataGrid.axaml`

- [ ] **Step 1: Add `ShortHash` to `RevisionRow.cs`**

In `RevisionRow.cs`, after the `AuthorDate` property, add:

```csharp
public string ShortHash => Revision.ObjectId.ToShortString();
```

- [ ] **Step 2: Update `RevisionDataGrid.axaml` — add header and data column**

In the header `Grid`, change `ColumnDefinitions="120,*,150,100"` to:

```
ColumnDefinitions="120,*,150,100,80"
```

And add a 5th header `TextBlock` after the Date one:

```xml
<TextBlock Grid.Column="4" Text="Hash"
           FontSize="11" FontWeight="SemiBold"
           Margin="4,2" VerticalAlignment="Center" />
```

In the item template `Grid`, change `ColumnDefinitions="120,*,150,100"` to:

```
ColumnDefinitions="120,*,150,100,80"
```

And add a 5th data `TextBlock` after the Date one:

```xml
<TextBlock Grid.Column="4"
           Text="{Binding ShortHash}"
           VerticalAlignment="Center"
           Margin="4,0"
           FontSize="11"
           Foreground="Gray"
           FontFamily="{StaticResource MonospaceFont}" />
```

- [ ] **Step 3: Build — expect 0 errors**

```bash
cd /Users/alon/Desktop/gitextensions
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS" | head -10
```

- [ ] **Step 4: Commit**

```bash
cd /Users/alon/Desktop/gitextensions
git add src/app/GitUI.Avalonia/RevisionGrid/RevisionRow.cs \
        src/app/GitUI.Avalonia/RevisionGrid/RevisionDataGrid.axaml
git commit -m "feat(avalonia): add short commit-hash column to revision grid"
```

---

## Task 9: File tree hierarchy in commit details

Replace the flat file list with a `TreeView` grouped by directory path.

**Files:**
- Create: `src/app/GitUI.Avalonia/CommitDetails/FileTreeNode.cs`
- Create: `tests/app/GitUI.Avalonia.Tests/CommitDetails/FileTreeNodeTests.cs`
- Modify: `src/app/GitUI.Avalonia/CommitDetails/FileStatusList.axaml`
- Modify: `src/app/GitUI.Avalonia/CommitDetails/FileStatusList.axaml.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/app/GitUI.Avalonia.Tests/CommitDetails/FileTreeNodeTests.cs`:

```csharp
using GitUI.Avalonia.CommitDetails;
using NUnit.Framework;

namespace GitUI.Avalonia.Tests.CommitDetails;

[TestFixture]
public class FileTreeNodeTests
{
    [Test]
    public void Build_FlatFile_ReturnsSingleRootChild()
    {
        var items = new[] { new FileStatusItem("README.md", false, false, false) };
        IReadOnlyList<FileTreeNode> nodes = FileTreeNode.BuildTree(items);
        Assert.That(nodes, Has.Count.EqualTo(1));
        Assert.That(nodes[0].Name, Is.EqualTo("README.md"));
        Assert.That(nodes[0].IsDirectory, Is.False);
    }

    [Test]
    public void Build_NestedFile_CreatesDirectoryNode()
    {
        var items = new[] { new FileStatusItem("src/Foo.cs", false, false, false) };
        IReadOnlyList<FileTreeNode> nodes = FileTreeNode.BuildTree(items);
        Assert.That(nodes, Has.Count.EqualTo(1));
        Assert.That(nodes[0].Name, Is.EqualTo("src"));
        Assert.That(nodes[0].IsDirectory, Is.True);
        Assert.That(nodes[0].Children, Has.Count.EqualTo(1));
        Assert.That(nodes[0].Children[0].Name, Is.EqualTo("Foo.cs"));
    }

    [Test]
    public void Build_SiblingFiles_GroupedUnderSameDirectory()
    {
        var items = new[]
        {
            new FileStatusItem("src/A.cs", false, false, false),
            new FileStatusItem("src/B.cs", false, false, false),
        };
        IReadOnlyList<FileTreeNode> nodes = FileTreeNode.BuildTree(items);
        Assert.That(nodes, Has.Count.EqualTo(1));
        Assert.That(nodes[0].Children, Has.Count.EqualTo(2));
    }

    [Test]
    public void Build_MixedDepths_RootFilesAndDirectories()
    {
        var items = new[]
        {
            new FileStatusItem("README.md", false, false, false),
            new FileStatusItem("src/Foo.cs", false, false, false),
        };
        IReadOnlyList<FileTreeNode> nodes = FileTreeNode.BuildTree(items);
        Assert.That(nodes, Has.Count.EqualTo(2));
    }
}
```

- [ ] **Step 2: Run test — expect compile error (FileTreeNode not yet defined)**

```bash
cd /Users/alon/Desktop/gitextensions
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj \
  --filter "FullyQualifiedName~FileTreeNodeTests" 2>&1 | tail -10
```

Expected: build error `The type or namespace name 'FileTreeNode' could not be found`.

- [ ] **Step 3: Create `FileTreeNode.cs`**

Create `src/app/GitUI.Avalonia/CommitDetails/FileTreeNode.cs`:

```csharp
namespace GitUI.Avalonia.CommitDetails;

public sealed class FileTreeNode
{
    public string Name { get; }
    public bool IsDirectory { get; }
    public string? FilePath { get; }
    public FileStatusItem? FileItem { get; }
    public List<FileTreeNode> Children { get; } = [];

    public string StatusIcon => IsDirectory ? "📁" : (FileItem?.StatusIcon ?? "");

    private FileTreeNode(string name, bool isDirectory, string? filePath = null, FileStatusItem? item = null)
    {
        Name = name;
        IsDirectory = isDirectory;
        FilePath = filePath;
        FileItem = item;
    }

    public static IReadOnlyList<FileTreeNode> BuildTree(IEnumerable<FileStatusItem> files)
    {
        var root = new Dictionary<string, FileTreeNode>(StringComparer.Ordinal);

        foreach (FileStatusItem file in files)
        {
            string[] parts = file.Name.Replace('\\', '/').Split('/');
            Dictionary<string, FileTreeNode> current = root;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                string dir = parts[i];
                if (!current.TryGetValue(dir, out FileTreeNode? dirNode))
                {
                    dirNode = new FileTreeNode(dir, isDirectory: true);
                    current[dir] = dirNode;
                }
                current = dirNode.Children
                    .Where(c => c.IsDirectory)
                    .ToDictionary(c => c.Name, StringComparer.Ordinal);

                // Rebuild lookup — keep a reference to the actual children dict
                current = GetChildrenDict(dirNode);
            }

            string fileName = parts[^1];
            current[fileName] = new FileTreeNode(fileName, isDirectory: false, file.Name, file);
        }

        return root.Values.OrderBy(n => !n.IsDirectory).ThenBy(n => n.Name).ToList();
    }

    private static Dictionary<string, FileTreeNode> GetChildrenDict(FileTreeNode node)
    {
        var dict = new Dictionary<string, FileTreeNode>(StringComparer.Ordinal);
        foreach (FileTreeNode child in node.Children)
            dict[child.Name] = child;
        return dict;
    }
}
```

Note: `BuildTree` above has an issue with the mutable child dict — replace the method body with this corrected version that uses a single recursive helper:

```csharp
public static IReadOnlyList<FileTreeNode> BuildTree(IEnumerable<FileStatusItem> files)
{
    var rootChildren = new Dictionary<string, FileTreeNode>(StringComparer.Ordinal);

    foreach (FileStatusItem file in files)
    {
        string[] parts = file.Name.Replace('\\', '/').Split('/');
        InsertFile(rootChildren, parts, 0, file);
    }

    return SortNodes(rootChildren.Values);
}

private static void InsertFile(
    Dictionary<string, FileTreeNode> children,
    string[] parts,
    int depth,
    FileStatusItem file)
{
    string name = parts[depth];

    if (depth == parts.Length - 1)
    {
        children[name] = new FileTreeNode(name, isDirectory: false, file.Name, file);
        return;
    }

    if (!children.TryGetValue(name, out FileTreeNode? dir))
    {
        dir = new FileTreeNode(name, isDirectory: true);
        children[name] = dir;
    }

    var childDict = dir.Children.ToDictionary(c => c.Name, StringComparer.Ordinal);
    InsertFile(childDict, parts, depth + 1, file);
    dir.Children.Clear();
    foreach (FileTreeNode child in SortNodes(childDict.Values))
        dir.Children.Add(child);
}

private static IReadOnlyList<FileTreeNode> SortNodes(IEnumerable<FileTreeNode> nodes) =>
    nodes.OrderBy(n => !n.IsDirectory).ThenBy(n => n.Name).ToList();
```

Replace the initial stub `BuildTree` with the corrected version above.

- [ ] **Step 4: Run tests — expect PASS**

```bash
cd /Users/alon/Desktop/gitextensions
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj \
  --filter "FullyQualifiedName~FileTreeNodeTests" 2>&1 | grep -E "Passed|Failed|Error"
```

Expected: `Passed! - Failed: 0`

- [ ] **Step 5: Replace `FileStatusList.axaml` with TreeView layout**

```xml
<local:GitModuleControl xmlns="https://github.com/avaloniaui"
                         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                         xmlns:local="using:GitUI.Avalonia.Base"
                         x:Class="GitUI.Avalonia.CommitDetails.FileStatusList">
    <TreeView x:Name="FileTree"
              SelectionChanged="FileTree_SelectionChanged">
        <TreeView.ItemTemplate>
            <TreeDataTemplate ItemsSource="{Binding Children}">
                <StackPanel Orientation="Horizontal" Spacing="4">
                    <TextBlock Text="{Binding StatusIcon}"
                               Width="16"
                               FontFamily="Cascadia Code,Menlo,Consolas,monospace" />
                    <TextBlock Text="{Binding Name}"
                               FontFamily="Cascadia Code,Menlo,Consolas,monospace"
                               FontSize="12" />
                </StackPanel>
            </TreeDataTemplate>
        </TreeView.ItemTemplate>
    </TreeView>
</local:GitModuleControl>
```

- [ ] **Step 6: Update `FileStatusList.axaml.cs`**

Replace the entire file content with:

```csharp
using Avalonia.Controls;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.CommitDetails;

public record FileStatusItem(string Name, bool IsAdded, bool IsDeleted, bool IsRenamed)
{
    public string StatusIcon => (IsAdded, IsDeleted, IsRenamed) switch
    {
        (true, _, _) => "A",
        (_, true, _) => "D",
        (_, _, true) => "R",
        _ => "M",
    };
}

public partial class FileStatusList : GitModuleControl
{
    public event Action<FileStatusItem?>? SelectedFileChanged;

    public FileStatusList() => InitializeComponent();

    public void LoadFiles(IEnumerable<FileStatusItem> files)
    {
        FileTree.ItemsSource = FileTreeNode.BuildTree(files);
    }

    private void FileTree_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        FileStatusItem? item = (FileTree.SelectedItem as FileTreeNode)?.FileItem;
        SelectedFileChanged?.Invoke(item);
    }
}
```

- [ ] **Step 7: Build — expect 0 errors**

```bash
cd /Users/alon/Desktop/gitextensions
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS" | head -10
```

- [ ] **Step 8: Run all tests**

```bash
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj 2>&1 | grep -E "Passed|Failed"
```

Expected: `Passed! - Failed: 0`

- [ ] **Step 9: Commit**

```bash
cd /Users/alon/Desktop/gitextensions
git add src/app/GitUI.Avalonia/CommitDetails/FileTreeNode.cs \
        src/app/GitUI.Avalonia/CommitDetails/FileStatusList.axaml \
        src/app/GitUI.Avalonia/CommitDetails/FileStatusList.axaml.cs \
        tests/app/GitUI.Avalonia.Tests/CommitDetails/FileTreeNodeTests.cs
git commit -m "feat(avalonia): show changed files as directory tree instead of flat list"
```

---

## Task 10: Recent repositories in File menu

Show the last 20 opened repos as a "Recent Repositories" submenu with one-click reopen.

**Files:**
- Modify: `src/app/GitUI.Avalonia/MainWindow.axaml.cs`

- [ ] **Step 1: Add `BuildRecentReposMenu()` and call it from `BuildFileMenu()`**

Add this method:

```csharp
private MenuItem BuildRecentReposMenu()
{
    var sub = new MenuItem { Header = "Recent _Repositories" };
    var recent = App.Settings.GetStringList("recentRepositories");

    if (recent.Count == 0)
    {
        sub.Items.Add(new MenuItem { Header = "(none)", IsEnabled = false });
        return sub;
    }

    foreach (string path in recent)
    {
        string display = path; // full path; shorten if desired
        sub.Items.Add(new MenuItem
        {
            Header = display,
            Command = ReactiveCommand.Create(() => OpenRepository(path)),
        });
    }

    return sub;
}
```

In `BuildFileMenu()`, after the `_Init New Repository_` item and before the first `Separator`, insert:

```csharp
menu.Items.Add(BuildRecentReposMenu());
```

- [ ] **Step 2: Build — expect 0 errors**

```bash
cd /Users/alon/Desktop/gitextensions
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS" | head -10
```

- [ ] **Step 3: Commit**

```bash
cd /Users/alon/Desktop/gitextensions
git add src/app/GitUI.Avalonia/MainWindow.axaml.cs
git commit -m "feat(avalonia): add recent repositories submenu to File menu"
```

---

## Task 11: Per-ref-type badge colors in revision grid

Color-code each ref badge: local branches = blue, remote branches = green, tags = gold.

**Files:**
- Create: `src/app/GitUI.Avalonia/Converters/RefTypeBrushConverter.cs`
- Create: `tests/app/GitUI.Avalonia.Tests/Converters/RefTypeBrushConverterTests.cs`
- Modify: `src/app/GitUI.Avalonia/AppTheme.axaml`
- Modify: `src/app/GitUI.Avalonia/RevisionGrid/RevisionDataGrid.axaml`

- [ ] **Step 1: Write failing test**

Create `tests/app/GitUI.Avalonia.Tests/Converters/RefTypeBrushConverterTests.cs`:

```csharp
using GitUI.Avalonia.Converters;
using GitExtensions.Extensibility.Git;
using NUnit.Framework;

namespace GitUI.Avalonia.Tests.Converters;

[TestFixture]
public class RefTypeBrushConverterTests
{
    [Test]
    public void RefTypeKey_LocalBranch_ReturnsLocalKey()
    {
        Assert.That(RefTypeBrushConverter.GetResourceKey(isHead: true, isRemote: false, isTag: false),
            Is.EqualTo("RefLabelLocalBranch"));
    }

    [Test]
    public void RefTypeKey_Remote_ReturnsRemoteKey()
    {
        Assert.That(RefTypeBrushConverter.GetResourceKey(isHead: false, isRemote: true, isTag: false),
            Is.EqualTo("RefLabelRemoteBranch"));
    }

    [Test]
    public void RefTypeKey_Tag_ReturnsTagKey()
    {
        Assert.That(RefTypeBrushConverter.GetResourceKey(isHead: false, isRemote: false, isTag: true),
            Is.EqualTo("RefLabelTag"));
    }

    [Test]
    public void RefTypeKey_Unknown_ReturnsLocalKey()
    {
        Assert.That(RefTypeBrushConverter.GetResourceKey(isHead: false, isRemote: false, isTag: false),
            Is.EqualTo("RefLabelLocalBranch"));
    }
}
```

- [ ] **Step 2: Run test — expect compile error**

```bash
cd /Users/alon/Desktop/gitextensions
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj \
  --filter "FullyQualifiedName~RefTypeBrushConverterTests" 2>&1 | tail -10
```

Expected: build error `RefTypeBrushConverter not found`.

- [ ] **Step 3: Create `RefTypeBrushConverter.cs`**

Create `src/app/GitUI.Avalonia/Converters/RefTypeBrushConverter.cs`:

```csharp
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using GitExtensions.Extensibility.Git;

namespace GitUI.Avalonia.Converters;

/// <summary>
/// Converts an IGitRef to the AppTheme brush resource key matching its type.
/// Used in XAML as a static method called from a multi-step binding or DataTrigger.
/// </summary>
public sealed class RefTypeBrushConverter : IValueConverter
{
    public static readonly RefTypeBrushConverter Instance = new();

    public static string GetResourceKey(bool isHead, bool isRemote, bool isTag) =>
        isTag    ? "RefLabelTag" :
        isRemote ? "RefLabelRemoteBranch" :
                   "RefLabelLocalBranch";

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IGitRef gitRef)
        {
            string key = GetResourceKey(gitRef.IsHead, gitRef.IsRemote, gitRef.IsTag);
            if (Avalonia.Application.Current?.TryFindResource(key, out object? res) == true && res is IBrush brush)
                return brush;
        }
        return Brushes.DodgerBlue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
```

- [ ] **Step 4: Run test — expect PASS**

```bash
cd /Users/alon/Desktop/gitextensions
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj \
  --filter "FullyQualifiedName~RefTypeBrushConverterTests" 2>&1 | grep -E "Passed|Failed"
```

Expected: `Passed! - Failed: 0`

- [ ] **Step 5: Register converter in `AppTheme.axaml`**

In `AppTheme.axaml`, after the existing converter registrations, add:

```xml
<local:RefTypeBrushConverter x:Key="RefTypeBrush" />
```

- [ ] **Step 6: Replace single-color ref label with per-ref `ItemsControl` in `RevisionDataGrid.axaml`**

In the item template `DataTemplate`, replace the entire `<StackPanel Grid.Column="1" ...>` block with:

```xml
<!-- Subject with per-ref-type colored badges -->
<StackPanel Grid.Column="1"
            Orientation="Horizontal"
            VerticalAlignment="Center"
            Spacing="4"
            Margin="4,0">
    <ItemsControl ItemsSource="{Binding Refs}">
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <StackPanel Orientation="Horizontal" Spacing="2" />
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <Border Background="#22000000"
                        CornerRadius="2"
                        Padding="3,1">
                    <TextBlock Text="{Binding LocalName}"
                               Foreground="{Binding ., Converter={StaticResource RefTypeBrush}}"
                               FontSize="10"
                               FontWeight="SemiBold"
                               VerticalAlignment="Center" />
                </Border>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
    <TextBlock Text="{Binding Subject}"
               VerticalAlignment="Center"
               TextTrimming="CharacterEllipsis" />
</StackPanel>
```

- [ ] **Step 7: Build — expect 0 errors**

```bash
cd /Users/alon/Desktop/gitextensions
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS" | head -10
```

- [ ] **Step 8: Run all tests**

```bash
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj 2>&1 | grep -E "Passed|Failed"
```

Expected: `Passed! - Failed: 0`

- [ ] **Step 9: Commit**

```bash
cd /Users/alon/Desktop/gitextensions
git add src/app/GitUI.Avalonia/Converters/RefTypeBrushConverter.cs \
        src/app/GitUI.Avalonia/AppTheme.axaml \
        src/app/GitUI.Avalonia/RevisionGrid/RevisionDataGrid.axaml \
        tests/app/GitUI.Avalonia.Tests/Converters/RefTypeBrushConverterTests.cs
git commit -m "feat(avalonia): color-code ref badges by type (local=blue, remote=green, tag=gold)"
```

---

## Spec Coverage Check

| Feature area | Task |
|---|---|
| Toolbar (refresh, commit, fetch, pull, push, stash) | Task 1 |
| Branch selector ComboBox | Task 1 |
| Left panel repo tree (branches, remotes, tags, stashes) | Task 2 |
| Branch checkout via left panel | Task 2, 6 |
| Tabbed commit details (Diff / Files, summary always visible) | Task 3 |
| Lazy diff load on tab switch | Task 3 |
| Toggle left panel | Task 4 |
| Action bars (merge/cherry-pick/bisect/revert in progress) | Task 5 |
| Ref context menus (checkout, merge, delete, push) | Task 6 |
| Tools menu (terminal, finder, settings) | Task 7 |
| View menu (toggle panel, refresh) | Task 7 |
| Navigate menu (go to commit) | Task 7 |
| CommitId column in grid | Task 8 |
| File tree hierarchy | Task 9 |
| Recent repos in File menu | Task 10 |
| Per-ref-type badge colors | Task 11 |
