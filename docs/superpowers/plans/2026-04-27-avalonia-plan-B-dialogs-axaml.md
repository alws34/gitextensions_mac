# Avalonia Mac Port — Plan B: Missing Dialog UIs + Dialog Completeness

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create the missing AXAML files for BlameDialog and FileHistoryDialog, wire them into the file context menu, add named-stash support, complete RebaseDialog with autostash/continue/abort, and add stash operations to the left panel.

**Architecture:** BlameDialog and FileHistoryDialog already have `.cs` logic — they just need `.axaml` layout files. Context menu wiring goes through `FileStatusList` events bubbled up to `CommitDetailsPanel`. Left-panel stash actions call `git stash apply/pop/drop` and refresh.

**Tech Stack:** Avalonia AXAML, AvaloniaEdit, C# async/await, git CLI

---

## File Map

| Action | File |
|---|---|
| Create | `src/app/GitUI.Avalonia/Dialogs/BlameDialog.axaml` |
| Create | `src/app/GitUI.Avalonia/Dialogs/FileHistoryDialog.axaml` |
| Modify | `src/app/GitUI.Avalonia/CommitDetails/FileStatusList.axaml` |
| Modify | `src/app/GitUI.Avalonia/CommitDetails/FileStatusList.axaml.cs` |
| Modify | `src/app/GitUI.Avalonia/CommitDetails/CommitDetailsPanel.axaml.cs` |
| Modify | `src/app/GitUI.Avalonia/Dialogs/StashDialog.axaml` |
| Modify | `src/app/GitUI.Avalonia/Dialogs/StashDialog.axaml.cs` |
| Modify | `src/app/GitUI.Avalonia/Dialogs/RebaseDialog.axaml` |
| Modify | `src/app/GitUI.Avalonia/Dialogs/RebaseDialog.axaml.cs` |
| Modify | `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml` |
| Modify | `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml.cs` |

---

## Task 1: BlameDialog AXAML

**Files:**
- Create: `src/app/GitUI.Avalonia/Dialogs/BlameDialog.axaml`

The existing `BlameDialog.axaml.cs` expects `BlameList` (ListBox) and `BlameEditor` (AvaloniaEdit TextEditor) to exist in the AXAML.

- [ ] **Step 1: Create `BlameDialog.axaml`**

```xml
<local:GitExtensionsDialog xmlns="https://github.com/avaloniaui"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                            xmlns:local="using:GitUI.Avalonia.Base"
                            xmlns:ae="clr-namespace:AvaloniaEdit;assembly=AvaloniaEdit"
                            x:Class="GitUI.Avalonia.Dialogs.BlameDialog"
                            Title="Blame"
                            Width="1000" Height="650"
                            SizeToContent="Manual" CanResize="True">
    <Grid ColumnDefinitions="280,5,*" Margin="8">
        <!-- Left: annotation list (hash + author per line) -->
        <DockPanel Grid.Column="0">
            <TextBlock DockPanel.Dock="Top"
                       Text="Annotations"
                       FontWeight="SemiBold"
                       Margin="0,0,0,4" />
            <ListBox x:Name="BlameList"
                     FontFamily="Cascadia Code,Menlo,Consolas,monospace"
                     FontSize="11"
                     ScrollViewer.HorizontalScrollBarVisibility="Auto" />
        </DockPanel>
        <GridSplitter Grid.Column="1" ResizeDirection="Columns" />
        <!-- Right: file content -->
        <ae:TextEditor Grid.Column="2"
                       x:Name="BlameEditor"
                       IsReadOnly="True"
                       FontFamily="Cascadia Code,Menlo,Consolas,monospace"
                       FontSize="12"
                       ShowLineNumbers="True"
                       WordWrap="False" />
    </Grid>
</local:GitExtensionsDialog>
```

- [ ] **Step 2: Build**

```bash
cd /Users/alon/Desktop/gitextensions
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS"
```

Expected: `Build succeeded`

- [ ] **Step 3: Commit**

```bash
git add src/app/GitUI.Avalonia/Dialogs/BlameDialog.axaml
git commit -m "feat(avalonia): add BlameDialog AXAML layout (annotations + editor)"
```

---

## Task 2: FileHistoryDialog AXAML

**Files:**
- Create: `src/app/GitUI.Avalonia/Dialogs/FileHistoryDialog.axaml`

The existing `FileHistoryDialog.axaml.cs` expects `CommitsList` (ListBox) and `FileEditor` (AvaloniaEdit TextEditor).

- [ ] **Step 1: Create `FileHistoryDialog.axaml`**

```xml
<local:GitExtensionsDialog xmlns="https://github.com/avaloniaui"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                            xmlns:local="using:GitUI.Avalonia.Base"
                            xmlns:ae="clr-namespace:AvaloniaEdit;assembly=AvaloniaEdit"
                            x:Class="GitUI.Avalonia.Dialogs.FileHistoryDialog"
                            Title="File History"
                            Width="900" Height="600"
                            SizeToContent="Manual" CanResize="True">
    <Grid ColumnDefinitions="300,5,*" Margin="8">
        <!-- Left: commit list -->
        <DockPanel Grid.Column="0">
            <TextBlock DockPanel.Dock="Top"
                       Text="Commits"
                       FontWeight="SemiBold"
                       Margin="0,0,0,4" />
            <ListBox x:Name="CommitsList"
                     FontFamily="Cascadia Code,Menlo,Consolas,monospace"
                     FontSize="11"
                     SelectionChanged="CommitsList_SelectionChanged" />
        </DockPanel>
        <GridSplitter Grid.Column="1" ResizeDirection="Columns" />
        <!-- Right: file content at selected revision -->
        <ae:TextEditor Grid.Column="2"
                       x:Name="FileEditor"
                       IsReadOnly="True"
                       FontFamily="Cascadia Code,Menlo,Consolas,monospace"
                       FontSize="12"
                       ShowLineNumbers="True"
                       WordWrap="False" />
    </Grid>
</local:GitExtensionsDialog>
```

- [ ] **Step 2: Build**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS"
```

- [ ] **Step 3: Commit**

```bash
git add src/app/GitUI.Avalonia/Dialogs/FileHistoryDialog.axaml
git commit -m "feat(avalonia): add FileHistoryDialog AXAML layout (commit list + editor)"
```

---

## Task 3: Wire Blame and File History from file context menu

**Files:**
- Modify: `src/app/GitUI.Avalonia/CommitDetails/FileStatusList.axaml`
- Modify: `src/app/GitUI.Avalonia/CommitDetails/FileStatusList.axaml.cs`
- Modify: `src/app/GitUI.Avalonia/CommitDetails/CommitDetailsPanel.axaml.cs`

`FileStatusList` raises events; `CommitDetailsPanel` opens the dialogs.

- [ ] **Step 1: Add context menu to FileStatusList.axaml**

Replace the current `<TreeView>` element with:

```xml
<TreeView x:Name="FileTree"
          SelectionChanged="FileTree_SelectionChanged">
    <TreeView.ContextMenu>
        <ContextMenu Opening="FileTree_ContextMenuOpening">
            <MenuItem Header="Open Diff"        Click="CtxDiff_Click" />
            <Separator />
            <MenuItem Header="File History…"    Click="CtxHistory_Click" />
            <MenuItem Header="Blame…"           Click="CtxBlame_Click" />
        </ContextMenu>
    </TreeView.ContextMenu>
    <TreeView.ItemTemplate>
        <TreeDataTemplate ItemsSource="{Binding Children}">
            <StackPanel Orientation="Horizontal" Spacing="4">
                <TextBlock Text="{Binding DisplayIcon}"
                           Width="14"
                           FontFamily="Cascadia Code,Menlo,Consolas,monospace" />
                <TextBlock Text="{Binding Name}"
                           FontFamily="Cascadia Code,Menlo,Consolas,monospace"
                           FontSize="12" />
            </StackPanel>
        </TreeDataTemplate>
    </TreeView.ItemTemplate>
</TreeView>
```

- [ ] **Step 2: Add events and handlers to FileStatusList.axaml.cs**

Add two public events after existing events:

```csharp
public event Action<string>? BlameRequested;
public event Action<string>? HistoryRequested;
```

Add context menu handlers:

```csharp
private void FileTree_ContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
{
    // Only enable file actions when a file node (not a directory) is selected
    bool hasFile = SelectedFile is not null;
    if (FileTree.ContextMenu is { } menu)
    {
        foreach (var item in menu.Items.OfType<MenuItem>())
        {
            item.IsEnabled = hasFile;
        }
    }
}

private void CtxDiff_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
{
    // Diff is already shown via SelectionChanged — nothing extra needed
}

private void CtxHistory_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
{
    if (SelectedFile?.Name is { } path)
        HistoryRequested?.Invoke(path);
}

private void CtxBlame_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
{
    if (SelectedFile?.Name is { } path)
        BlameRequested?.Invoke(path);
}
```

Add a helper property to expose the selected `FileStatusItem`. Read the existing `FileTree_SelectionChanged` method to see how `SelectedFile` is already tracked — if `_selectedFile` already exists, just expose it:

```csharp
public FileStatusItem? SelectedFile => _selectedFile;
```

If `_selectedFile` doesn't exist yet, add it as a field and set it in `FileTree_SelectionChanged`.

- [ ] **Step 3: Subscribe in CommitDetailsPanel.axaml.cs**

In `CommitDetailsPanel.axaml.cs`, the constructor or `SetModule` method is where setup happens. Add subscription to the new events. Read the file first to find the right location, then add:

```csharp
FileList.BlameRequested += path =>
{
    if (_module is not null)
        new GitUI.Avalonia.Dialogs.BlameDialog(_module, path).Show();
};
FileList.HistoryRequested += path =>
{
    if (_module is not null)
        new GitUI.Avalonia.Dialogs.FileHistoryDialog(_module, path).Show();
};
```

- [ ] **Step 4: Build + test**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS"
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj 2>&1 | grep -E "Passed|Failed"
```

- [ ] **Step 5: Commit**

```bash
git add src/app/GitUI.Avalonia/CommitDetails/FileStatusList.axaml \
        src/app/GitUI.Avalonia/CommitDetails/FileStatusList.axaml.cs \
        src/app/GitUI.Avalonia/CommitDetails/CommitDetailsPanel.axaml.cs
git commit -m "feat(avalonia): wire Blame and File History from file context menu"
```

---

## Task 4: StashDialog — named stash message

**Files:**
- Modify: `src/app/GitUI.Avalonia/Dialogs/StashDialog.axaml`
- Modify: `src/app/GitUI.Avalonia/Dialogs/StashDialog.axaml.cs`

Read the current StashDialog.axaml to find the Stash button and add a message TextBox above it.

- [ ] **Step 1: Read current StashDialog.axaml**

```bash
cat src/app/GitUI.Avalonia/Dialogs/StashDialog.axaml
```

- [ ] **Step 2: Add message input**

Find the `Stash` button area and add above it:

```xml
<TextBlock Text="Stash message (optional):" Margin="0,4,0,2" />
<TextBox x:Name="StashMessageBox" Watermark="WIP on feature…" Margin="0,0,0,4" />
```

- [ ] **Step 3: Update StashSaveAsync in StashDialog.axaml.cs**

Replace `StashSaveAsync`:

```csharp
private async System.Threading.Tasks.Task StashSaveAsync()
{
    string msg = await Dispatcher.UIThread.InvokeAsync(() =>
        StashMessageBox.Text?.Trim() ?? string.Empty);

    string args = string.IsNullOrEmpty(msg)
        ? "stash push"
        : $"stash push -m \"{msg}\"";

    await System.Threading.Tasks.Task.Run(() => _module.GitExecutable.GetOutput(args));
    await LoadStashesAsync();
    await Dispatcher.UIThread.InvokeAsync(() => StashMessageBox.Text = string.Empty);
}
```

- [ ] **Step 4: Build + commit**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS"
git add src/app/GitUI.Avalonia/Dialogs/StashDialog.axaml \
        src/app/GitUI.Avalonia/Dialogs/StashDialog.axaml.cs
git commit -m "feat(avalonia): add named stash message to StashDialog"
```

---

## Task 5: RebaseDialog — autostash + continue/abort/skip

**Files:**
- Modify: `src/app/GitUI.Avalonia/Dialogs/RebaseDialog.axaml`
- Modify: `src/app/GitUI.Avalonia/Dialogs/RebaseDialog.axaml.cs`

- [ ] **Step 1: Read current RebaseDialog.axaml**

```bash
cat src/app/GitUI.Avalonia/Dialogs/RebaseDialog.axaml
```

- [ ] **Step 2: Add autostash checkbox and continue/abort buttons**

After `InteractiveCheckBox` (or the last checkbox), add:

```xml
<CheckBox x:Name="AutostashCheckBox" Content="Auto stash before rebase" />
<Separator Margin="0,8" />
<TextBlock Text="If a rebase is already in progress:" FontStyle="Italic" FontSize="11" />
<StackPanel Orientation="Horizontal" Spacing="8" Margin="0,4,0,0">
    <Button Content="Continue"  Click="Continue_Click" />
    <Button Content="Skip"      Click="Skip_Click" />
    <Button Content="Abort"     Click="Abort_Click" />
</StackPanel>
```

- [ ] **Step 3: Update RebaseAsync and add Continue/Skip/Abort in RebaseDialog.axaml.cs**

Replace `RebaseAsync`:

```csharp
private async System.Threading.Tasks.Task RebaseAsync()
{
    string onto = BranchComboBox.SelectedItem?.ToString() ?? string.Empty;
    if (string.IsNullOrEmpty(onto))
        return;

    string interactive = InteractiveCheckBox.IsChecked == true ? "-i " : string.Empty;
    string autostash   = AutostashCheckBox.IsChecked == true   ? "--autostash " : string.Empty;
    await System.Threading.Tasks.Task.Run(() =>
        _module.GitExecutable.GetOutput($"rebase {interactive}{autostash}{onto}".TrimEnd()));
    Close(true);
}

private void Continue_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    => _ = RunRebaseControlAsync("--continue");

private void Skip_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    => _ = RunRebaseControlAsync("--skip");

private void Abort_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    => _ = RunRebaseControlAsync("--abort");

private async System.Threading.Tasks.Task RunRebaseControlAsync(string flag)
{
    await System.Threading.Tasks.Task.Run(() =>
        _module.GitExecutable.GetOutput($"rebase {flag}"));
    Close(true);
}
```

- [ ] **Step 4: Build + commit**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS"
git add src/app/GitUI.Avalonia/Dialogs/RebaseDialog.axaml \
        src/app/GitUI.Avalonia/Dialogs/RebaseDialog.axaml.cs
git commit -m "feat(avalonia): add autostash + continue/skip/abort to RebaseDialog"
```

---

## Task 6: Left panel — stash double-click and context menu

**Files:**
- Modify: `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml`
- Modify: `src/app/GitUI.Avalonia/LeftPanel/RepoBrowserPanel.axaml.cs`

Allow users to apply/pop/drop stashes directly from the left panel.

- [ ] **Step 1: Add DoubleTapped and ContextMenu to StashesList in RepoBrowserPanel.axaml**

Replace the current `<ListBox x:Name="StashesList" />` with:

```xml
<ListBox x:Name="StashesList"
         DoubleTapped="Stash_DoubleTapped">
    <ListBox.ContextMenu>
        <ContextMenu Opening="StashMenu_Opening">
            <MenuItem Header="Pop (apply + drop)"  Click="Stash_Pop" />
            <MenuItem Header="Apply (keep stash)"  Click="Stash_Apply" />
            <Separator />
            <MenuItem Header="Drop…"               Click="Stash_Drop" />
        </ContextMenu>
    </ListBox.ContextMenu>
</ListBox>
```

- [ ] **Step 2: Add handlers in RepoBrowserPanel.axaml.cs**

The stash entries in `StashesList` are stored as `GitStash` objects. The stash ref is `stash@{index}`. Add these handlers:

```csharp
private void Stash_DoubleTapped(object? sender, global::Avalonia.Input.TappedEventArgs e)
{
    int idx = StashesList.SelectedIndex;
    if (idx >= 0)
        _ = StashPopAsync(idx);
}

private void StashMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
{
    bool hasSelection = StashesList.SelectedIndex >= 0;
    if (sender is ContextMenu menu)
    {
        foreach (var item in menu.Items.OfType<MenuItem>())
            item.IsEnabled = hasSelection;
    }
}

private void Stash_Pop(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
{
    int idx = StashesList.SelectedIndex;
    if (idx >= 0)
        _ = StashPopAsync(idx);
}

private void Stash_Apply(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
{
    int idx = StashesList.SelectedIndex;
    if (idx >= 0)
        _ = StashApplyAsync(idx);
}

private void Stash_Drop(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
{
    int idx = StashesList.SelectedIndex;
    if (idx >= 0)
        _ = StashDropAsync(idx);
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
    catch (Exception ex) { ErrorOccurred?.Invoke(ex.Message); }
}

private async System.Threading.Tasks.Task StashApplyAsync(int index)
{
    try
    {
        await System.Threading.Tasks.Task.Run(() =>
            _module!.GitExecutable.GetOutput($"stash apply stash@{{{index}}}"));
        StatusRequested?.Invoke("Stash applied");
    }
    catch (Exception ex) { ErrorOccurred?.Invoke(ex.Message); }
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
git commit -m "feat(avalonia): stash pop/apply/drop from left panel double-tap and context menu"
```
