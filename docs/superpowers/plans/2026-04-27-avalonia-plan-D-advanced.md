# Avalonia Mac Port — Plan D: Advanced Features

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete the remaining advanced features — Go To Commit navigation, revision grid search/filter, submodule and worktree sections in the left panel, and branch rename from the left panel.

**Architecture:** GoToCommit resolves an ObjectId and scrolls the `RevisionDataGrid` to that row. Filter bar connects to `RevisionGridControl` via a new `SetFilter` method. Left panel submodule/worktree sections use `git submodule status` and `git worktree list`. Branch rename is an additional left-panel context menu item.

**Tech Stack:** C# async/await, Avalonia AXAML, git CLI, `ObjectId.TryParse`

---

## File Map

| Action | File |
|---|---|
| Modify | `src/app/GitUI.Avalonia/Dialogs/GoToCommitDialog.axaml` |
| Modify | `src/app/GitUI.Avalonia/Dialogs/GoToCommitDialog.axaml.cs` |
| Modify | `src/app/GitUI.Avalonia/MainWindow.axaml.cs` |
| Modify | `src/app/GitUI.Avalonia/RevisionGrid/RevisionGridControl.axaml.cs` |
| Modify | `src/app/GitUI.Avalonia/RevisionGrid/RevisionDataGrid.axaml.cs` |
| Modify | `src/app/GitUI.Avalonia/Controls/FilterToolBar.axaml` |
| Modify | `src/app/GitUI.Avalonia/Controls/FilterToolBar.axaml.cs` |
| Modify | `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml` |
| Modify | `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml.cs` |

---

## Task 1: Go To Commit — navigate grid to a hash

**Files:**
- Modify: `src/app/GitUI.Avalonia/Dialogs/GoToCommitDialog.axaml`
- Modify: `src/app/GitUI.Avalonia/Dialogs/GoToCommitDialog.axaml.cs`
- Modify: `src/app/GitUI.Avalonia/MainWindow.axaml.cs`
- Modify: `src/app/GitUI.Avalonia/RevisionGrid/RevisionDataGrid.axaml.cs`

- [ ] **Step 1: Read current GoToCommitDialog files**

```bash
cat src/app/GitUI.Avalonia/Dialogs/GoToCommitDialog.axaml
cat src/app/GitUI.Avalonia/Dialogs/GoToCommitDialog.axaml.cs
```

- [ ] **Step 2: Ensure GoToCommitDialog has a hash TextBox and returns the hash**

If the AXAML is empty or scaffold-only, replace with:

```xml
<local:GitExtensionsDialog xmlns="https://github.com/avaloniaui"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                            xmlns:local="using:GitUI.Avalonia.Base"
                            x:Class="GitUI.Avalonia.Dialogs.GoToCommitDialog"
                            Title="Go to Commit"
                            Width="420">
    <StackPanel Margin="16" Spacing="8">
        <TextBlock Text="Enter commit hash or short hash:" />
        <TextBox x:Name="HashBox"
                 Watermark="e.g. abc1234 or full SHA"
                 TextChanged="HashBox_TextChanged" />
        <TextBlock x:Name="ErrorLabel"
                   Foreground="Red"
                   FontSize="11"
                   IsVisible="False" />
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right"
                    Spacing="8" Margin="0,8,0,0">
            <Button Content="Cancel" Click="Cancel_Click" />
            <Button x:Name="GoButton" Content="Go" IsDefault="True"
                    IsEnabled="False" Click="Go_Click" />
        </StackPanel>
    </StackPanel>
</local:GitExtensionsDialog>
```

Replace `GoToCommitDialog.axaml.cs` with:

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class GoToCommitDialog : GitExtensionsDialog
{
    public string? ResultHash { get; private set; }

    public GoToCommitDialog()
    {
        InitializeComponent();
    }

    private void HashBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        string text = HashBox.Text?.Trim() ?? string.Empty;
        bool valid = text.Length >= 4 &&
                     text.All(c => (c >= '0' && c <= '9') ||
                                   (c >= 'a' && c <= 'f') ||
                                   (c >= 'A' && c <= 'F'));
        GoButton.IsEnabled = valid;
        ErrorLabel.IsVisible = text.Length > 0 && !valid;
        ErrorLabel.Text = "Enter a valid hex hash (at least 4 characters)";
    }

    private void Go_Click(object? sender, RoutedEventArgs e)
    {
        ResultHash = HashBox.Text?.Trim();
        Close(ResultHash);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
}
```

- [ ] **Step 3: Add `ScrollToHash` to RevisionDataGrid.axaml.cs**

Read `src/app/GitUI.Avalonia/RevisionGrid/RevisionDataGrid.axaml.cs`, then add:

```csharp
public void ScrollToHash(string shortHash)
{
    // Find the first row whose ShortHash starts with the input
    var rows = CommitList.ItemsSource as System.Collections.IList;
    if (rows is null)
        return;

    for (int i = 0; i < rows.Count; i++)
    {
        if (rows[i] is RevisionRow row &&
            row.ShortHash.StartsWith(shortHash, StringComparison.OrdinalIgnoreCase))
        {
            CommitList.SelectedIndex = i;
            CommitList.ScrollIntoView(CommitList.SelectedItem!);
            return;
        }
    }
}
```

- [ ] **Step 4: Wire GoToCommit in MainWindow**

Find `BuildNavigateMenu()` in `MainWindow.axaml.cs`. Replace the current `GoToCommitDialog` call with a version that uses the result:

```csharp
private MenuItem BuildNavigateMenu()
{
    var menu = new MenuItem { Header = "_Navigate" };
    menu.Items.Add(new MenuItem
    {
        Header = "_Go to Commit…",
        Command = ReactiveCommand.CreateFromTask(GoToCommitAsync),
    });
    return menu;
}

private async System.Threading.Tasks.Task GoToCommitAsync()
{
    var dialog = new GoToCommitDialog();
    var result = await dialog.ShowDialog<string?>(this);
    if (result is { Length: >= 4 })
        RevisionGrid.ScrollToHash(result);
}
```

Also add the public method to `RevisionGridControl.axaml.cs`:

```csharp
public void ScrollToHash(string shortHash) => DataGrid.ScrollToHash(shortHash);
```

- [ ] **Step 5: Build + run all tests**

```bash
cd /Users/alon/Desktop/gitextensions
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS"
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj 2>&1 | grep -E "Passed|Failed"
```

- [ ] **Step 6: Commit**

```bash
git add src/app/GitUI.Avalonia/Dialogs/GoToCommitDialog.axaml \
        src/app/GitUI.Avalonia/Dialogs/GoToCommitDialog.axaml.cs \
        src/app/GitUI.Avalonia/RevisionGrid/RevisionDataGrid.axaml.cs \
        src/app/GitUI.Avalonia/RevisionGrid/RevisionGridControl.axaml.cs \
        src/app/GitUI.Avalonia/MainWindow.axaml.cs
git commit -m "feat(avalonia): Go To Commit dialog navigates revision grid to matching hash"
```

---

## Task 2: Filter bar — connect search to revision grid

**Files:**
- Modify: `src/app/GitUI.Avalonia/Controls/FilterToolBar.axaml`
- Modify: `src/app/GitUI.Avalonia/Controls/FilterToolBar.axaml.cs`
- Modify: `src/app/GitUI.Avalonia/RevisionGrid/RevisionGridControl.axaml.cs`

- [ ] **Step 1: Read current FilterToolBar files**

```bash
cat src/app/GitUI.Avalonia/Controls/FilterToolBar.axaml
cat src/app/GitUI.Avalonia/Controls/FilterToolBar.axaml.cs
```

- [ ] **Step 2: Ensure FilterToolBar.axaml has a search TextBox and fires an event**

If the AXAML is incomplete, replace with:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="GitUI.Avalonia.Controls.FilterToolBar">
    <Grid ColumnDefinitions="Auto,*,Auto,Auto" Margin="4,2">
        <TextBlock Grid.Column="0"
                   Text="Filter:"
                   VerticalAlignment="Center"
                   Margin="0,0,6,0" />
        <TextBox Grid.Column="1"
                 x:Name="FilterBox"
                 Watermark="message, author, or hash…"
                 TextChanged="FilterBox_TextChanged" />
        <ComboBox Grid.Column="2"
                  x:Name="FilterTypeCombo"
                  Margin="4,0,0,0"
                  Width="110"
                  SelectedIndex="0">
            <ComboBoxItem Content="All" />
            <ComboBoxItem Content="Author" />
            <ComboBoxItem Content="Message" />
            <ComboBoxItem Content="Hash" />
        </ComboBox>
        <Button Grid.Column="3"
                Content="✕"
                Margin="4,0,0,0"
                Padding="4,2"
                Click="Clear_Click"
                ToolTip.Tip="Clear filter" />
    </Grid>
</UserControl>
```

Replace `FilterToolBar.axaml.cs` with:

```csharp
using Avalonia.Controls;

namespace GitUI.Avalonia.Controls;

public partial class FilterToolBar : UserControl
{
    public event Action<string, FilterType>? FilterChanged;

    public FilterToolBar()
    {
        InitializeComponent();
    }

    private void FilterBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        string text = FilterBox.Text ?? string.Empty;
        FilterType type = FilterTypeCombo.SelectedIndex switch
        {
            1 => FilterType.Author,
            2 => FilterType.Message,
            3 => FilterType.Hash,
            _ => FilterType.All,
        };
        FilterChanged?.Invoke(text, type);
    }

    private void Clear_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        FilterBox.Text = string.Empty;
    }
}

public enum FilterType { All, Author, Message, Hash }
```

- [ ] **Step 3: Add client-side filtering to RevisionGridControl**

Read `src/app/GitUI.Avalonia/RevisionGrid/RevisionGridControl.axaml.cs`. Add a `SetFilter` method:

```csharp
private List<RevisionRow> _allRows = [];

// In LoadRevisionsAsync, after building rows, save them:
// _allRows = rows;
// Then call ApplyFilter() instead of DataGrid.LoadRevisions(rows) directly.

public void SetFilter(string text, Controls.FilterType type)
{
    if (string.IsNullOrWhiteSpace(text))
    {
        DataGrid.LoadRevisions(_allRows);
        return;
    }

    var filtered = _allRows.Where(r => type switch
    {
        Controls.FilterType.Author  => r.Author.Contains(text, StringComparison.OrdinalIgnoreCase),
        Controls.FilterType.Hash    => r.ShortHash.StartsWith(text, StringComparison.OrdinalIgnoreCase),
        Controls.FilterType.Message => r.Subject.Contains(text, StringComparison.OrdinalIgnoreCase),
        _                           => r.Author.Contains(text, StringComparison.OrdinalIgnoreCase)
                                     || r.Subject.Contains(text, StringComparison.OrdinalIgnoreCase)
                                     || r.ShortHash.StartsWith(text, StringComparison.OrdinalIgnoreCase),
    }).ToList();

    DataGrid.LoadRevisions(filtered);
}
```

Update `LoadRevisionsAsync` to save rows:

```csharp
_allRows = rows;
await Dispatcher.UIThread.InvokeAsync(() => DataGrid.LoadRevisions(rows));
```

- [ ] **Step 4: Wire filter bar to grid in MainWindow.axaml.cs**

In `OpenRepository`, after `RevisionGrid.Module = _module;`, add:

```csharp
FilterBar.FilterChanged += (text, type) => RevisionGrid.SetFilter(text, type);
```

- [ ] **Step 5: Build + run all tests**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS"
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj 2>&1 | grep -E "Passed|Failed"
```

- [ ] **Step 6: Commit**

```bash
git add src/app/GitUI.Avalonia/Controls/FilterToolBar.axaml \
        src/app/GitUI.Avalonia/Controls/FilterToolBar.axaml.cs \
        src/app/GitUI.Avalonia/RevisionGrid/RevisionGridControl.axaml.cs \
        src/app/GitUI.Avalonia/MainWindow.axaml.cs
git commit -m "feat(avalonia): wire filter bar to revision grid (author/message/hash search)"
```

---

## Task 3: Left panel — Submodules section

**Files:**
- Modify: `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml`
- Modify: `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml.cs`

Show submodules in the left panel. Double-clicking opens the submodule as a new repository.

- [ ] **Step 1: Add Submodules expander to RepoBrowserPanel.axaml**

Inside the `<StackPanel>` that contains the Expanders, add after the Stashes expander:

```xml
<Expander Header="Submodules">
    <ListBox x:Name="SubmodulesList"
             DoubleTapped="Submodule_DoubleTapped">
        <ListBox.ContextMenu>
            <ContextMenu>
                <MenuItem Header="Open Submodule" Click="Submodule_Open" />
                <MenuItem Header="Update"          Click="Submodule_Update" />
            </ContextMenu>
        </ListBox.ContextMenu>
    </ListBox>
</Expander>
```

- [ ] **Step 2: Load submodules and add handlers in RepoBrowserPanel.axaml.cs**

In `RefreshAsync()`, after loading stashes, add:

```csharp
// Load submodules
string submoduleOutput = await System.Threading.Tasks.Task.Run(() =>
    _module!.GitExecutable.GetOutput("submodule status"));

var submoduleNames = submoduleOutput
    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
    .Select(line =>
    {
        // Format: " hash path (description)" — extract the path
        string[] parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[1] : string.Empty;
    })
    .Where(s => s.Length > 0)
    .ToList();

await Dispatcher.UIThread.InvokeAsync(() =>
{
    SubmodulesList.ItemsSource = submoduleNames;
});
```

Add handlers:

```csharp
private void Submodule_DoubleTapped(object? sender, global::Avalonia.Input.TappedEventArgs e)
{
    if (SubmodulesList.SelectedItem is string name)
        OpenSubmodule(name);
}

private void Submodule_Open(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
{
    if (SubmodulesList.SelectedItem is string name)
        OpenSubmodule(name);
}

private void OpenSubmodule(string name)
{
    if (_module is null)
        return;
    string path = System.IO.Path.Combine(_module.WorkingDir, name);
    CheckoutRequested?.Invoke(path);   // reuse CheckoutRequested to signal open
}

private void Submodule_Update(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
{
    if (SubmodulesList.SelectedItem is string name)
        _ = UpdateSubmoduleAsync(name);
}

private async System.Threading.Tasks.Task UpdateSubmoduleAsync(string name)
{
    try
    {
        string result = await System.Threading.Tasks.Task.Run(() =>
            _module!.GitExecutable.GetOutput($"submodule update --init -- \"{name}\""));
        StatusRequested?.Invoke(result.Trim());
    }
    catch (Exception ex) { ErrorOccurred?.Invoke(ex.Message); }
}
```

- [ ] **Step 3: Build + run all tests**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS"
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj 2>&1 | grep -E "Passed|Failed"
```

- [ ] **Step 4: Commit**

```bash
git add src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml \
        src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml.cs
git commit -m "feat(avalonia): submodules section in left panel with update action"
```

---

## Task 4: Left panel — Worktrees section

**Files:**
- Modify: `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml`
- Modify: `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml.cs`

- [ ] **Step 1: Add Worktrees expander to RepoBrowserPanel.axaml**

After Submodules expander, add:

```xml
<Expander Header="Worktrees">
    <ListBox x:Name="WorktreesList"
             DoubleTapped="Worktree_DoubleTapped" />
</Expander>
```

- [ ] **Step 2: Load worktrees in RefreshAsync**

After submodule loading, add:

```csharp
// Load worktrees
string worktreeOutput = await System.Threading.Tasks.Task.Run(() =>
    _module!.GitExecutable.GetOutput("worktree list --porcelain"));

var worktreePaths = worktreeOutput
    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
    .Where(l => l.StartsWith("worktree ", StringComparison.Ordinal))
    .Select(l => l["worktree ".Length..].Trim())
    .Where(p => p != _module!.WorkingDir.TrimEnd('/'))
    .ToList();

await Dispatcher.UIThread.InvokeAsync(() =>
{
    WorktreesList.ItemsSource = worktreePaths;
});
```

Add handler:

```csharp
private void Worktree_DoubleTapped(object? sender, global::Avalonia.Input.TappedEventArgs e)
{
    if (WorktreesList.SelectedItem is string path)
        CheckoutRequested?.Invoke(path);   // signal MainWindow to open that worktree path
}
```

- [ ] **Step 3: Build + commit**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS"
git add src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml \
        src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml.cs
git commit -m "feat(avalonia): worktrees section in left panel"
```

---

## Task 5: Left panel — Rename branch context menu item

**Files:**
- Modify: `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml`
- Modify: `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml.cs`

- [ ] **Step 1: Add Rename to LocalBranchesList ContextMenu**

Read `RepoBrowserPanel.axaml`. In the `LocalBranchesList` ContextMenu, add after "Delete Branch…":

```xml
<MenuItem Header="_Rename Branch…" Click="LocalBranch_Rename" />
```

- [ ] **Step 2: Add handler in RepoBrowserPanel.axaml.cs**

```csharp
private void LocalBranch_Rename(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
{
    if (LocalBranchesList.SelectedItem is string branch && _module is not null)
        _ = RenameBranchAsync(branch);
}

private async System.Threading.Tasks.Task RenameBranchAsync(string oldName)
{
    // Show the rename dialog
    var dialog = new GitUI.Avalonia.Dialogs.RenameBranchDialog(_module!);
    // Pre-select the current branch
    await dialog.ShowDialog<object?>(
        (global::Avalonia.Application.Current?.ApplicationLifetime
            as global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)
        ?.MainWindow);
    await RefreshAsync();
}
```

- [ ] **Step 3: Build + run all tests**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS"
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj 2>&1 | grep -E "Passed|Failed"
```

- [ ] **Step 4: Commit**

```bash
git add src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml \
        src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml.cs
git commit -m "feat(avalonia): rename branch from left panel context menu"
```
