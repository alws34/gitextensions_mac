# Mac Port — Plan C: Screen Wave 1 (Dialogs — Parallel)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port all git operation dialogs — commit, branches, remotes, tags, stash, settings, submodules, worktrees, conflict resolution, and patches.

**Architecture:** All tasks in this plan are FULLY INDEPENDENT and can run in parallel. Each is a self-contained dialog with no dependency on another task in this plan. All depend only on Plan A + Plan B being complete.

**Prerequisite:** Plans A and B must be complete.

**Pattern for every dialog in this plan:**
1. Read the WinForms source (`Form*.cs` + `.Designer.cs`)
2. Create `Dialogs/FooDialog.axaml` inheriting `GitExtensionsDialog`
3. Port layout from Designer file using the `WINFORMS-TO-AVALONIA.md` cheat sheet
4. Port event handlers from `.cs` file — keep logic, replace UI calls
5. Verify: dialog opens, shows real data, buttons work
6. Commit

**Reference docs:**
- `docs/mac-port/AGENT-GUIDE.md`
- `docs/mac-port/WINFORMS-TO-AVALONIA.md`
- `docs/superpowers/specs/2026-04-19-mac-port-design.md` Section 6

---

## How to read this plan

Each task below follows this structure. Rather than repeating the full boilerplate for all 50 dialogs, the pattern is documented once here and referenced per-task.

### Standard dialog pattern

**AXAML template:**
```xml
<local:GitExtensionsDialog xmlns="https://github.com/avaloniaui"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                            xmlns:local="using:GitUI.Avalonia.Base"
                            x:Class="GitUI.Avalonia.Dialogs.FooDialog"
                            Title="[Dialog Title]"
                            Width="[width]">
    <!-- port layout from Designer.cs here -->
    <StackPanel Margin="16" Spacing="8">
        <!-- controls -->
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Spacing="8" Margin="0,8,0,0">
            <Button Content="Cancel" Click="Cancel_Click" />
            <Button Content="OK" IsDefault="True" Click="OK_Click" />
        </StackPanel>
    </StackPanel>
</local:GitExtensionsDialog>
```

**Code-behind template:**
```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class FooDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public FooDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        LoadData();
    }

    private void LoadData()
    {
        // Load data from _module into controls
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        // Execute git command via _module
        // Then: Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
```

**Done criteria for every dialog:**
- [ ] Dialog opens without exceptions
- [ ] Controls populate with real data from the git module
- [ ] Primary action (OK/Apply) executes the correct git command
- [ ] Cancel closes without doing anything
- [ ] `dotnet build GitExtensions.Mac.slnx` — 0 errors after this dialog

---

## Task C1: Commit Dialog

**Source:** `src/app/GitUI/CommandsDialogs/FormCommit.cs` (large — read carefully)

**Output:** `src/app/GitUI.Avalonia/Dialogs/CommitDialog.axaml` + `.axaml.cs`

This is the most complex dialog in this batch. Key elements:
- Left panel: unstaged files list + staged files list (two `FileStatusList` controls reused from Plan B)
- Right panel: diff preview of selected file
- Bottom: commit message editor (AvaloniaEdit TextEditor)
- Buttons: Stage All, Unstage All, Commit, Amend checkbox

- [ ] **Step 1: Create `Dialogs/CommitDialog.axaml`**

```xml
<local:GitExtensionsDialog xmlns="https://github.com/avaloniaui"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                            xmlns:local="using:GitUI.Avalonia.Base"
                            xmlns:ae="clr-namespace:AvaloniaEdit;assembly=AvaloniaEdit"
                            x:Class="GitUI.Avalonia.Dialogs.CommitDialog"
                            Title="Commit" Width="1000" Height="700">
    <Grid RowDefinitions="*,200">
        <!-- Top: file lists + diff -->
        <Grid Grid.Row="0" ColumnDefinitions="250,5,250,5,*">
            <DockPanel Grid.Column="0">
                <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Spacing="4" Margin="4">
                    <TextBlock Text="Unstaged" FontWeight="SemiBold" />
                    <Button Content="Stage All" Click="StageAll_Click" Padding="4,2" />
                </StackPanel>
                <ListBox x:Name="UnstagedList"
                         SelectionChanged="UnstagedFile_Selected" />
            </DockPanel>
            <GridSplitter Grid.Column="1" ResizeDirection="Columns" />
            <DockPanel Grid.Column="2">
                <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Spacing="4" Margin="4">
                    <TextBlock Text="Staged" FontWeight="SemiBold" />
                    <Button Content="Unstage All" Click="UnstageAll_Click" Padding="4,2" />
                </StackPanel>
                <ListBox x:Name="StagedList"
                         SelectionChanged="StagedFile_Selected" />
            </DockPanel>
            <GridSplitter Grid.Column="3" ResizeDirection="Columns" />
            <ae:TextEditor Grid.Column="4" x:Name="DiffPreview"
                           IsReadOnly="True"
                           FontFamily="{DynamicResource MonospaceFont}"
                           FontSize="12" />
        </Grid>
        <!-- Bottom: commit message + buttons -->
        <Grid Grid.Row="1" RowDefinitions="Auto,*,Auto" Margin="8">
            <StackPanel Grid.Row="0" Orientation="Horizontal" Spacing="8">
                <CheckBox x:Name="AmendCheckBox" Content="Amend last commit" />
            </StackPanel>
            <ae:TextEditor Grid.Row="1" x:Name="CommitMessageEditor"
                           FontFamily="{DynamicResource MonospaceFont}"
                           FontSize="13"
                           Margin="0,4" />
            <StackPanel Grid.Row="2" Orientation="Horizontal"
                        HorizontalAlignment="Right" Spacing="8" Margin="0,8,0,4">
                <Button Content="Cancel" Click="Cancel_Click" />
                <Button Content="Commit" IsDefault="True" Click="Commit_Click" />
            </StackPanel>
        </Grid>
    </Grid>
</local:GitExtensionsDialog>
```

- [ ] **Step 2: Create `Dialogs/CommitDialog.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class CommitDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public CommitDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        _ = LoadStatusAsync();
    }

    private async Task LoadStatusAsync()
    {
        var (unstaged, staged) = await Task.Run(() =>
        {
            var unstaged = _module.GetUnstagedFiles();
            var staged = _module.GetStagedFiles();
            return (unstaged, staged);
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            UnstagedList.ItemsSource = unstaged.Select(f => f.Name).ToList();
            StagedList.ItemsSource = staged.Select(f => f.Name).ToList();
        });
    }

    private void StageAll_Click(object? sender, RoutedEventArgs e)
    {
        _module.StageAll();
        _ = LoadStatusAsync();
    }

    private void UnstageAll_Click(object? sender, RoutedEventArgs e)
    {
        _module.UnstageAll();
        _ = LoadStatusAsync();
    }

    private async void UnstagedFile_Selected(object? sender, SelectionChangedEventArgs e)
    {
        if (UnstagedList.SelectedItem is not string fileName) return;
        var diff = await Task.Run(() => _module.GetCurrentChanges(fileName, staged: false, extraDiffArguments: ""));
        DiffPreview.Text = diff?.Text ?? "";
    }

    private async void StagedFile_Selected(object? sender, SelectionChangedEventArgs e)
    {
        if (StagedList.SelectedItem is not string fileName) return;
        var diff = await Task.Run(() => _module.GetCurrentChanges(fileName, staged: true, extraDiffArguments: ""));
        DiffPreview.Text = diff?.Text ?? "";
    }

    private void Commit_Click(object? sender, RoutedEventArgs e)
    {
        var message = CommitMessageEditor.Text;
        if (string.IsNullOrWhiteSpace(message)) return;

        bool amend = AmendCheckBox.IsChecked == true;
        _module.Commit(new GitCommands.CommitData { Message = message, Amend = amend });
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
```

> **Note:** Check `GitModule` for the actual method names: `GetUnstagedFiles()`, `GetStagedFiles()`, `StageAll()`, `UnstageAll()`, `GetCurrentChanges()`, `Commit()`. They exist in the codebase but the exact signatures may differ — read `FormCommit.cs` to see how it calls them.

- [ ] **Step 3: Verify commit dialog**

```bash
dotnet run --project src/app/GitUI.Avalonia/GitUI.Avalonia.csproj
```

Wire the Commit menu item in `MainWindow` to open `CommitDialog`:
```csharp
new MenuItem { Header = "_Commit...", Command = ReactiveUI.ReactiveCommand.CreateFromTask(async () => {
    if (_module is null) return;
    await new CommitDialog(_module).ShowDialog<bool?>(this);
}) }
```

1. Open a repo with uncommitted changes
2. Commands > Commit → dialog opens
3. Unstaged files list populates
4. Stage All moves files
5. Type a message → Commit → dialog closes

- [ ] **Step 4: Commit**

```bash
git add src/app/GitUI.Avalonia/Dialogs/CommitDialog.axaml \
        src/app/GitUI.Avalonia/Dialogs/CommitDialog.axaml.cs
git commit -m "feat: add CommitDialog with staging, diff preview, commit action"
```

---

## Task C2: Branch Operations (7 dialogs — do all in one task)

**Source files to read:**
- `FormCheckoutBranch.cs`, `FormCreateBranch.cs`, `FormDeleteBranch.cs`
- `FormDeleteRemoteBranch.cs`, `FormRenameBranch.cs`, `FormMergeBranch.cs`, `FormRebase.cs`

**Output:** 7 files in `src/app/GitUI.Avalonia/Dialogs/`

These are all simple form-dialog pairs. Follow the standard dialog pattern above.

- [ ] **Step 1: Create `Dialogs/CheckoutBranchDialog.axaml` + `.axaml.cs`**

Layout: ComboBox with local branches list, "Create new branch" checkbox + text field, Checkout button.
Action: `_module.Checkout(branchName)` (check exact signature in `FormCheckoutBranch.cs`).

- [ ] **Step 2: Create `Dialogs/CreateBranchDialog.axaml` + `.axaml.cs`**

Layout: TextBox for branch name, ComboBox for base branch/commit, "Checkout after create" checkbox.
Action: `_module.CreateBranch(name, checkout: true)`.

- [ ] **Step 3: Create `Dialogs/DeleteBranchDialog.axaml` + `.axaml.cs`**

Layout: ListBox of local branches (multi-select), Force delete checkbox.
Action: `_module.DeleteBranch(name, force)`.

- [ ] **Step 4: Create `Dialogs/DeleteRemoteBranchDialog.axaml` + `.axaml.cs`**

Layout: ComboBox for remote, ListBox for remote branches.
Action: `_module.DeleteRemoteBranch(remote, branch)`.

- [ ] **Step 5: Create `Dialogs/RenameBranchDialog.axaml` + `.axaml.cs`**

Layout: Current name (read-only), new name TextBox.
Action: `_module.RenameBranch(oldName, newName)`.

- [ ] **Step 6: Create `Dialogs/MergeBranchDialog.axaml` + `.axaml.cs`**

Layout: Branch to merge ComboBox, merge strategy options (fast-forward only, no fast-forward, squash), commit message TextBox.
Action: `_module.Merge(branchName, mergeStrategy)`.

- [ ] **Step 7: Create `Dialogs/RebaseDialog.axaml` + `.axaml.cs`**

Layout: Branch to rebase onto ComboBox, interactive checkbox.
Action: `_module.Rebase(onto)`.

- [ ] **Step 8: Wire all 7 dialogs into the Commands menu in MainWindow**

Add menu items for: Checkout Branch, Create Branch, Delete Branch, Merge Branch, Rebase.

- [ ] **Step 9: Smoke test each dialog**

For each: open the dialog, verify the branch list populates, verify the action executes without error.

- [ ] **Step 10: Commit**

```bash
git add src/app/GitUI.Avalonia/Dialogs/Checkout* \
        src/app/GitUI.Avalonia/Dialogs/Create* \
        src/app/GitUI.Avalonia/Dialogs/Delete* \
        src/app/GitUI.Avalonia/Dialogs/Rename* \
        src/app/GitUI.Avalonia/Dialogs/Merge* \
        src/app/GitUI.Avalonia/Dialogs/Rebase*
git commit -m "feat: add branch operation dialogs (checkout, create, delete, rename, merge, rebase)"
```

---

## Task C3: Remote Operations (Clone, Pull, Push, Remotes)

**Source files:** `FormClone.cs`, `FormPull.cs`, `FormPush.cs`, `FormRemotes.cs` + `FormRemotesController.cs`

**Output:** `src/app/GitUI.Avalonia/Dialogs/CloneDialog.axaml`, `PullDialog.axaml`, `PushDialog.axaml`, `RemotesDialog.axaml`

- [ ] **Step 1: Create `Dialogs/CloneDialog.axaml` + `.axaml.cs`**

Layout: Source URL TextBox, destination path TextBox + Browse button, branch TextBox (optional), depth TextBox (optional), Clone button.
Action: Run `git clone <url> <path>` via `_module.GitExecutable` or GitCommands clone method.

- [ ] **Step 2: Create `Dialogs/PullDialog.axaml` + `.axaml.cs`**

Layout: Remote ComboBox, branch TextBox, merge strategy (merge/rebase/fetch only), Pull button.
Action: Call GitCommands pull method.

- [ ] **Step 3: Create `Dialogs/PushDialog.axaml` + `.axaml.cs`**

Layout: Remote ComboBox, local branch → remote branch mapping, force push checkbox (with warning), Push button.
Action: Call GitCommands push method.

- [ ] **Step 4: Create `Dialogs/RemotesDialog.axaml` + `.axaml.cs`**

Layout: ListBox of remotes, Add/Edit/Delete buttons, URL text box for selected remote.
Action: `_module.AddRemote()`, `_module.RemoveRemote()`, `_module.RenameRemote()`.

- [ ] **Step 5: Wire into Repository menu in MainWindow**

- [ ] **Step 6: Smoke test each dialog**

- [ ] **Step 7: Commit**

```bash
git add src/app/GitUI.Avalonia/Dialogs/Clone* \
        src/app/GitUI.Avalonia/Dialogs/Pull* \
        src/app/GitUI.Avalonia/Dialogs/Push* \
        src/app/GitUI.Avalonia/Dialogs/Remote*
git commit -m "feat: add remote operation dialogs (clone, pull, push, manage remotes)"
```

---

## Task C4: Tags, Stash, Cherry-pick, Revert

**Source files:** `FormCreateTag.cs`, `FormDeleteTag.cs`, `FormStash.cs`, `FormCherryPick.cs`, `FormRevertCommit.cs`

- [ ] **Step 1: `Dialogs/CreateTagDialog.axaml` + `.axaml.cs`**

Layout: Tag name TextBox, commit to tag at (ComboBox default=HEAD), annotated/lightweight choice, message TextBox (for annotated), Create button.

- [ ] **Step 2: `Dialogs/DeleteTagDialog.axaml` + `.axaml.cs`**

Layout: ListBox of tags (multi-select), Delete button.

- [ ] **Step 3: `Dialogs/StashDialog.axaml` + `.axaml.cs`**

Layout: ListBox of stashes, Stash/Apply/Pop/Drop buttons, diff preview of selected stash.

- [ ] **Step 4: `Dialogs/CherryPickDialog.axaml` + `.axaml.cs`**

Layout: Commit hash TextBox, No-commit checkbox, Cherry-pick button.

- [ ] **Step 5: `Dialogs/RevertCommitDialog.axaml` + `.axaml.cs`**

Layout: Commit info display, No-commit checkbox, Revert button.

- [ ] **Step 6: Commit**

```bash
git add src/app/GitUI.Avalonia/Dialogs/CreateTag* \
        src/app/GitUI.Avalonia/Dialogs/DeleteTag* \
        src/app/GitUI.Avalonia/Dialogs/Stash* \
        src/app/GitUI.Avalonia/Dialogs/CherryPick* \
        src/app/GitUI.Avalonia/Dialogs/Revert*
git commit -m "feat: add tag, stash, cherry-pick, revert dialogs"
```

---

## Task C5: Settings

**Source files:** `FormSettings.cs`, all `SettingsDialog/Pages/` files

**Output:** `src/app/GitUI.Avalonia/Settings/`

- [ ] **Step 1: Create `Settings/SettingsWindow.axaml` + `.axaml.cs`**

Layout: Left panel = TreeView of settings categories. Right panel = content area that swaps based on selection.

Categories (match WinForms structure):
- Git (git binary path)
- Appearance (fonts, colors, diff colors)
- Commit (template, auto-stage options)
- Build server integration
- Advanced

- [ ] **Step 2: Create `Settings/Pages/GitPage.axaml` + `.axaml.cs`**

Layout: "Git executable" path TextBox + Browse button + Test button that runs `git --version`.
Reads/writes: `App.Settings.GetString("gitBinDir", "")`.

- [ ] **Step 3: Create `Settings/Pages/AppearancePage.axaml` + `.axaml.cs`**

Layout: Font selector for editor, diff color pickers (added/removed/section).
Reads/writes: `App.Settings.GetString("editorFont", "Menlo")`, diff color settings.

- [ ] **Step 4: Wire Save/Cancel into SettingsWindow**

On Save: call `App.Settings.Save()` and close.
On Cancel: revert in-memory changes (re-load from backend).

- [ ] **Step 5: Wire Settings into File menu**

```csharp
new MenuItem { Header = "_Settings...", Command = ReactiveUI.ReactiveCommand.CreateFromTask(async () =>
    await new Settings.SettingsWindow().ShowDialog<bool?>(this)) }
```

- [ ] **Step 6: Smoke test**

Open Settings, change git path, save. Re-open — value persists.

- [ ] **Step 7: Commit**

```bash
git add src/app/GitUI.Avalonia/Settings/
git commit -m "feat: add Settings window with git path and appearance pages"
```

---

## Task C6: Submodules and Worktrees

**Source files:** `FormSubmodules.cs`, `FormAddSubmodule.cs`, `FormMergeSubmodule.cs`, `FormCreateWorktree.cs`, `FormManageWorktree.cs`

- [ ] **Step 1: `Dialogs/SubmodulesDialog.axaml` + `.axaml.cs`**

Layout: ListBox of submodules (name, path, branch), Update/Sync/Add/Remove buttons.

- [ ] **Step 2: `Dialogs/AddSubmoduleDialog.axaml` + `.axaml.cs`**

Layout: URL TextBox, local path TextBox, branch TextBox, Add button.

- [ ] **Step 3: `Dialogs/CreateWorktreeDialog.axaml` + `.axaml.cs`**

Layout: Path TextBox + Browse, branch TextBox or ComboBox, Create button.

- [ ] **Step 4: `Dialogs/ManageWorktreeDialog.axaml` + `.axaml.cs`**

Layout: ListBox of worktrees (path, branch, HEAD), Remove/Open buttons.

- [ ] **Step 5: Commit**

```bash
git add src/app/GitUI.Avalonia/Dialogs/*ubmodule* \
        src/app/GitUI.Avalonia/Dialogs/*orktree*
git commit -m "feat: add submodule and worktree dialogs"
```

---

## Task C7: Conflict Resolution and Patches

**Source files:** `FormResolveConflicts.cs`, `FormApplyPatch.cs`, `FormFormatPatch.cs`, `FormViewPatch.cs`

- [ ] **Step 1: `Dialogs/ResolveConflictsDialog.axaml` + `.axaml.cs`**

Layout: DataGrid of conflicted files (columns: path, conflict type), Resolve with ours/theirs/merge tool buttons, Mark Resolved button.
Action: Call `_module.StartMergeTool(filePath)` for selected file.

- [ ] **Step 2: `Dialogs/ApplyPatchDialog.axaml` + `.axaml.cs`**

Layout: File path TextBox + Browse (*.patch), whitespace options, Apply button.

- [ ] **Step 3: `Dialogs/FormatPatchDialog.axaml` + `.axaml.cs`**

Layout: From/To commit range, output directory, format options.

- [ ] **Step 4: `Dialogs/ViewPatchDialog.axaml` + `.axaml.cs`**

Layout: File picker for .patch file, AvaloniaEdit viewer showing patch contents.

- [ ] **Step 5: Commit**

```bash
git add src/app/GitUI.Avalonia/Dialogs/ResolveConflicts* \
        src/app/GitUI.Avalonia/Dialogs/*Patch*
git commit -m "feat: add conflict resolution and patch dialogs"
```

---

## Task C8: Repo Maintenance Dialogs

**Source files:** `FormInit.cs`, `FormCleanupRepository.cs`, `FormGitIgnore.cs`, `FormGitAttributes.cs`, `FormArchive.cs`, `FormVerify.cs`, `FormBisect.cs`, `FormReflog.cs`

Each follows the standard dialog pattern. Key notes:

- **FormInit**: Path TextBox + Browse, Init button → `GitModule.Init(path)`
- **FormCleanupRepository**: Checkboxes for what to clean (untracked files, ignored files, etc.), dry-run option, Clean button
- **FormGitIgnore**: AvaloniaEdit editor with `.gitignore` file contents, Save button
- **FormGitAttributes**: AvaloniaEdit editor with `.gitattributes` file contents, Save button
- **FormArchive**: Format ComboBox (zip/tar), path TextBox, commit ComboBox, Archive button
- **FormVerify**: Run `git fsck` output shown in a read-only TextBlock, Run button
- **FormBisect**: Start/Good/Bad/Reset buttons, current bisect state display
- **FormReflog**: DataGrid of reflog entries (action, hash, message), Checkout selected button

- [ ] **Step 1–8:** Create each dialog following the standard pattern

- [ ] **Step 9: Commit**

```bash
git add src/app/GitUI.Avalonia/Dialogs/Init* \
        src/app/GitUI.Avalonia/Dialogs/Cleanup* \
        src/app/GitUI.Avalonia/Dialogs/GitIgnore* \
        src/app/GitUI.Avalonia/Dialogs/GitAttributes* \
        src/app/GitUI.Avalonia/Dialogs/Archive* \
        src/app/GitUI.Avalonia/Dialogs/Verify* \
        src/app/GitUI.Avalonia/Dialogs/Bisect* \
        src/app/GitUI.Avalonia/Dialogs/Reflog*
git commit -m "feat: add repo maintenance dialogs (init, cleanup, gitignore, archive, bisect, reflog)"
```

---

## Plan C Done Criteria

- [ ] All listed dialogs open without exceptions
- [ ] Each dialog loads real data from the active git module
- [ ] Primary actions execute the correct git operations
- [ ] All dialogs accessible from MainWindow menus
- [ ] `dotnet build GitExtensions.Mac.slnx` — 0 errors
- [ ] `grep -r "System.Windows.Forms" src/app/GitUI.Avalonia/` — empty
- [ ] `docs/mac-port/TASK-STATUS.md` — Tasks 6–13 marked `complete`

Plan D (`2026-04-19-mac-port-plan-d-wave-2-and-plugins.md`) covers remaining helper dialogs, small git dialogs, and all plugins.
