# Avalonia Mac Port — Plan A: Diff Coloring + Per-file Staging

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the diff viewer show colored +/- lines and let users stage/unstage individual files in the commit dialog.

**Architecture:** A `DocumentColorizingTransformer` subclass colors diff lines in every AvaloniaEdit instance. The commit dialog gets double-tap and context-menu handlers for per-file staging. A `CommitMade` event on `CommitDialog` lets `MainWindow` refresh the grid after commit.

**Tech Stack:** AvaloniaEdit `DocumentColorizingTransformer`, C# events, `git add -- <file>`, `git reset HEAD -- <file>`

---

## File Map

| Action | File |
|---|---|
| Create | `src/app/GitUI.Avalonia/CommitDetails/DiffLineColorizer.cs` |
| Create | `tests/app/GitUI.Avalonia.Tests/CommitDetails/DiffLineColorizerTests.cs` |
| Modify | `src/app/GitUI.Avalonia/CommitDetails/CommitDiffControl.axaml.cs` |
| Modify | `src/app/GitUI.Avalonia/Dialogs/CommitDialog.axaml` |
| Modify | `src/app/GitUI.Avalonia/Dialogs/CommitDialog.axaml.cs` |
| Modify | `src/app/GitUI.Avalonia/MainWindow.axaml.cs` |

---

## Task 1: DiffLineColorizer — line type logic

**Files:**
- Create: `src/app/GitUI.Avalonia/CommitDetails/DiffLineColorizer.cs`
- Create: `tests/app/GitUI.Avalonia.Tests/CommitDetails/DiffLineColorizerTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/app/GitUI.Avalonia.Tests/CommitDetails/DiffLineColorizerTests.cs`:

```csharp
using GitUI.Avalonia.CommitDetails;
using NUnit.Framework;

namespace GitUI.Avalonia.Tests.CommitDetails;

[TestFixture]
public class DiffLineColorizerTests
{
    [TestCase("+added line", DiffLineType.Added)]
    [TestCase("+++  new file", DiffLineType.FileHeader)]
    [TestCase("-removed line", DiffLineType.Removed)]
    [TestCase("--- a/foo.cs", DiffLineType.FileHeader)]
    [TestCase("@@ -1,3 +1,4 @@", DiffLineType.Section)]
    [TestCase(" context line", DiffLineType.Context)]
    [TestCase("diff --git a/foo b/foo", DiffLineType.Context)]
    [TestCase("", DiffLineType.Context)]
    public void GetLineType_ReturnsExpectedType(string line, DiffLineType expected)
    {
        Assert.That(DiffLineColorizer.GetLineType(line), Is.EqualTo(expected));
    }
}
```

- [ ] **Step 2: Run test — expect compile error**

```bash
cd /Users/alon/Desktop/gitextensions
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj \
  --filter "FullyQualifiedName~DiffLineColorizerTests" 2>&1 | tail -5
```

Expected: build error `DiffLineColorizer not found`.

- [ ] **Step 3: Create `DiffLineColorizer.cs`**

Create `src/app/GitUI.Avalonia/CommitDetails/DiffLineColorizer.cs`:

```csharp
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace GitUI.Avalonia.CommitDetails;

public enum DiffLineType { Added, Removed, Section, FileHeader, Context }

public sealed class DiffLineColorizer : DocumentColorizingTransformer
{
    private static readonly IBrush AddedBg    = new SolidColorBrush(Color.FromArgb(55, 0, 180, 0));
    private static readonly IBrush RemovedBg  = new SolidColorBrush(Color.FromArgb(55, 200, 0, 0));
    private static readonly IBrush SectionBg  = new SolidColorBrush(Color.FromArgb(40, 180, 180, 0));

    public static DiffLineType GetLineType(string line)
    {
        if (line.Length == 0)                                   return DiffLineType.Context;
        if (line.StartsWith("+++") || line.StartsWith("---"))  return DiffLineType.FileHeader;
        if (line[0] == '+')                                     return DiffLineType.Added;
        if (line[0] == '-')                                     return DiffLineType.Removed;
        if (line.StartsWith("@@"))                              return DiffLineType.Section;
        return DiffLineType.Context;
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (line.Length == 0)
            return;

        string text = CurrentContext.Document.GetText(line.Offset, Math.Min(line.Length, 3));
        IBrush? bg = GetLineType(text) switch
        {
            DiffLineType.Added    => AddedBg,
            DiffLineType.Removed  => RemovedBg,
            DiffLineType.Section  => SectionBg,
            _                     => null,
        };

        if (bg is null)
            return;

        ChangeLinePart(line.Offset, line.EndOffset, el => el.BackgroundBrush = bg);
    }
}
```

- [ ] **Step 4: Run test — expect PASS**

```bash
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj \
  --filter "FullyQualifiedName~DiffLineColorizerTests" 2>&1 | grep -E "Passed|Failed"
```

Expected: `Passed! - Failed: 0`

- [ ] **Step 5: Commit**

```bash
git add src/app/GitUI.Avalonia/CommitDetails/DiffLineColorizer.cs \
        tests/app/GitUI.Avalonia.Tests/CommitDetails/DiffLineColorizerTests.cs
git commit -m "feat(avalonia): add DiffLineColorizer for green/red diff backgrounds"
```

---

## Task 2: Apply colorizer to CommitDiffControl

**Files:**
- Modify: `src/app/GitUI.Avalonia/CommitDetails/CommitDiffControl.axaml.cs`

Current constructor is just `InitializeComponent()`. We need to add the colorizer to `DiffEditor` after init.

- [ ] **Step 1: Modify `CommitDiffControl.axaml.cs`**

Replace the constructor body:

```csharp
public CommitDiffControl()
{
    InitializeComponent();
    DiffEditor.TextArea.TextView.LineTransformers.Add(new DiffLineColorizer());
}
```

Full file after change:

```csharp
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.CommitDetails;

public partial class CommitDiffControl : GitModuleControl
{
    public CommitDiffControl()
    {
        InitializeComponent();
        DiffEditor.TextArea.TextView.LineTransformers.Add(new DiffLineColorizer());
    }

    public async System.Threading.Tasks.Task ShowDiffAsync(GitRevision revision, string? filePath = null)
    {
        if (Module is null)
            return;

        try
        {
            string gitArgs = filePath is null
                ? $"diff-tree --no-commit-id -p {revision.Guid}"
                : $"diff-tree --no-commit-id -p {revision.Guid} -- \"{filePath}\"";

            string diff = await Module.GitExecutable.GetOutputAsync(gitArgs);
            await Dispatcher.UIThread.InvokeAsync(() => DiffEditor.Text = diff);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShowDiffAsync failed: {ex.Message}");
        }
    }
}
```

- [ ] **Step 2: Build**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS"
```

Expected: `Build succeeded`

- [ ] **Step 3: Commit**

```bash
git add src/app/GitUI.Avalonia/CommitDetails/CommitDiffControl.axaml.cs
git commit -m "feat(avalonia): apply diff line colorizer to CommitDiffControl"
```

---

## Task 3: Apply colorizer to CommitDialog diff preview

**Files:**
- Modify: `src/app/GitUI.Avalonia/Dialogs/CommitDialog.axaml.cs`

The `CommitDialog` has a `DiffPreview` AvaloniaEdit instance. Add the colorizer in the constructor.

- [ ] **Step 1: Modify constructor in `CommitDialog.axaml.cs`**

Change the constructor to:

```csharp
public CommitDialog(GitModule module)
{
    _module = module;
    InitializeComponent();
    DiffPreview.TextArea.TextView.LineTransformers.Add(new DiffLineColorizer());
    _ = LoadStatusAsync();
}
```

Add the using at the top:

```csharp
using GitUI.Avalonia.CommitDetails;
```

- [ ] **Step 2: Build**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS"
```

- [ ] **Step 3: Commit**

```bash
git add src/app/GitUI.Avalonia/Dialogs/CommitDialog.axaml.cs
git commit -m "feat(avalonia): apply diff line colorizer to CommitDialog preview"
```

---

## Task 4: Per-file stage/unstage via double-tap

**Files:**
- Modify: `src/app/GitUI.Avalonia/Dialogs/CommitDialog.axaml`
- Modify: `src/app/GitUI.Avalonia/Dialogs/CommitDialog.axaml.cs`

Double-tapping an unstaged file stages it (`git add -- "<file>"`). Double-tapping a staged file unstages it (`git reset HEAD -- "<file>"`).

- [ ] **Step 1: Add DoubleTapped handlers to CommitDialog.axaml**

In the AXAML, change both ListBox elements to add `DoubleTapped`:

```xml
<ListBox x:Name="UnstagedList"
         SelectionChanged="UnstagedFile_Selected"
         DoubleTapped="UnstagedFile_DoubleTapped" />
```

```xml
<ListBox x:Name="StagedList"
         SelectionChanged="StagedFile_Selected"
         DoubleTapped="StagedFile_DoubleTapped" />
```

Full updated AXAML — replace the two `<DockPanel>` blocks (columns 0 and 2):

```xml
<DockPanel Grid.Column="0">
    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Spacing="4" Margin="4">
        <TextBlock Text="Unstaged" FontWeight="SemiBold" VerticalAlignment="Center" />
        <Button Content="Stage All" Click="StageAll_Click" Padding="4,2" />
    </StackPanel>
    <ListBox x:Name="UnstagedList"
             SelectionChanged="UnstagedFile_Selected"
             DoubleTapped="UnstagedFile_DoubleTapped" />
</DockPanel>
```

```xml
<DockPanel Grid.Column="2">
    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Spacing="4" Margin="4">
        <TextBlock Text="Staged" FontWeight="SemiBold" VerticalAlignment="Center" />
        <Button Content="Unstage All" Click="UnstageAll_Click" Padding="4,2" />
    </StackPanel>
    <ListBox x:Name="StagedList"
             SelectionChanged="StagedFile_Selected"
             DoubleTapped="StagedFile_DoubleTapped" />
</DockPanel>
```

- [ ] **Step 2: Add handlers to CommitDialog.axaml.cs**

Add these two methods to `CommitDialog.axaml.cs`:

```csharp
private void UnstagedFile_DoubleTapped(object? sender, global::Avalonia.Input.TappedEventArgs e)
{
    if (UnstagedList.SelectedItem is string fileName)
        _ = StageFileAsync(fileName);
}

private void StagedFile_DoubleTapped(object? sender, global::Avalonia.Input.TappedEventArgs e)
{
    if (StagedList.SelectedItem is string fileName)
        _ = UnstageFileAsync(fileName);
}

private async System.Threading.Tasks.Task StageFileAsync(string fileName)
{
    await System.Threading.Tasks.Task.Run(() =>
        _module.GitExecutable.GetOutput($"add -- \"{fileName}\""));
    await LoadStatusAsync();
    await Dispatcher.UIThread.InvokeAsync(() => DiffPreview.Text = string.Empty);
}

private async System.Threading.Tasks.Task UnstageFileAsync(string fileName)
{
    await System.Threading.Tasks.Task.Run(() =>
        _module.GitExecutable.GetOutput($"reset HEAD -- \"{fileName}\""));
    await LoadStatusAsync();
    await Dispatcher.UIThread.InvokeAsync(() => DiffPreview.Text = string.Empty);
}
```

- [ ] **Step 3: Build**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS"
```

- [ ] **Step 4: Commit**

```bash
git add src/app/GitUI.Avalonia/Dialogs/CommitDialog.axaml \
        src/app/GitUI.Avalonia/Dialogs/CommitDialog.axaml.cs
git commit -m "feat(avalonia): per-file stage/unstage via double-tap in commit dialog"
```

---

## Task 5: Right-click context menu for file lists in CommitDialog

**Files:**
- Modify: `src/app/GitUI.Avalonia/Dialogs/CommitDialog.axaml`

Add Stage / Unstage context menus to each list.

- [ ] **Step 1: Add ContextMenu to UnstagedList**

```xml
<ListBox x:Name="UnstagedList"
         SelectionChanged="UnstagedFile_Selected"
         DoubleTapped="UnstagedFile_DoubleTapped">
    <ListBox.ContextMenu>
        <ContextMenu>
            <MenuItem Header="Stage File" Click="CtxStage_Click" />
            <MenuItem Header="Show Diff"  Click="CtxDiffUnstaged_Click" />
        </ContextMenu>
    </ListBox.ContextMenu>
</ListBox>
```

Add ContextMenu to StagedList:

```xml
<ListBox x:Name="StagedList"
         SelectionChanged="StagedFile_Selected"
         DoubleTapped="StagedFile_DoubleTapped">
    <ListBox.ContextMenu>
        <ContextMenu>
            <MenuItem Header="Unstage File" Click="CtxUnstage_Click" />
            <MenuItem Header="Show Diff"    Click="CtxDiffStaged_Click" />
        </ContextMenu>
    </ListBox.ContextMenu>
</ListBox>
```

- [ ] **Step 2: Add handlers to CommitDialog.axaml.cs**

```csharp
private void CtxStage_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
{
    if (UnstagedList.SelectedItem is string f)
        _ = StageFileAsync(f);
}

private void CtxUnstage_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
{
    if (StagedList.SelectedItem is string f)
        _ = UnstageFileAsync(f);
}

private void CtxDiffUnstaged_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
{
    if (UnstagedList.SelectedItem is string f)
        _ = ShowDiffAsync(f, staged: false);
}

private void CtxDiffStaged_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
{
    if (StagedList.SelectedItem is string f)
        _ = ShowDiffAsync(f, staged: true);
}
```

- [ ] **Step 3: Build + run all tests**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS"
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj 2>&1 | grep -E "Passed|Failed"
```

- [ ] **Step 4: Commit**

```bash
git add src/app/GitUI.Avalonia/Dialogs/CommitDialog.axaml \
        src/app/GitUI.Avalonia/Dialogs/CommitDialog.axaml.cs
git commit -m "feat(avalonia): right-click context menu for stage/unstage in commit dialog"
```

---

## Task 6: Commit success → close dialog and refresh revision grid

**Files:**
- Modify: `src/app/GitUI.Avalonia/Dialogs/CommitDialog.axaml.cs`
- Modify: `src/app/GitUI.Avalonia/MainWindow.axaml.cs`

When commit succeeds, the dialog fires `CommitMade`. MainWindow subscribes and refreshes the grid.

- [ ] **Step 1: Add `CommitMade` event to CommitDialog**

In `CommitDialog.axaml.cs`, add after the field declarations:

```csharp
public event Action? CommitMade;
```

In `CommitAsync()`, replace the final `if (success)` block with:

```csharp
if (success)
{
    CommitMade?.Invoke();
    await Dispatcher.UIThread.InvokeAsync(() => Close(true));
}
else
{
    await Dispatcher.UIThread.InvokeAsync(() => DiffPreview.Text = result);
}
```

- [ ] **Step 2: Add `ShowCommitDialogAsync` to MainWindow**

In `MainWindow.axaml.cs`, add this method:

```csharp
private async System.Threading.Tasks.Task ShowCommitDialogAsync()
{
    if (_module is null)
        return;
    var dialog = new CommitDialog(_module);
    dialog.CommitMade += () =>
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await RevisionGrid.RefreshAsync();
            await RefreshBranchSelectorAsync();
        });
    };
    await dialog.ShowDialog<object?>(this);
}
```

- [ ] **Step 3: Wire up toolbar Commit button and menu item**

In `BuildToolBar()`, change:

```csharp
MainToolBar.Children.Add(MakeToolButton("Commit…", "Commit staged changes",
    () => _ = ShowModuleDialogAsync(m => new CommitDialog(m))));
```

to:

```csharp
MainToolBar.Children.Add(MakeToolButton("Commit…", "Commit staged changes",
    () => _ = ShowCommitDialogAsync()));
```

In `BuildCommandsMenu()`, change:

```csharp
menu.Items.Add(new MenuItem { Header = "_Commit...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new CommitDialog(m))) });
```

to:

```csharp
menu.Items.Add(new MenuItem { Header = "_Commit...", Command = ReactiveCommand.CreateFromTask(ShowCommitDialogAsync) });
```

- [ ] **Step 4: Build + run all tests**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS"
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj 2>&1 | grep -E "Passed|Failed"
```

- [ ] **Step 5: Commit**

```bash
git add src/app/GitUI.Avalonia/Dialogs/CommitDialog.axaml.cs \
        src/app/GitUI.Avalonia/MainWindow.axaml.cs
git commit -m "feat(avalonia): close commit dialog and refresh grid after successful commit"
```
