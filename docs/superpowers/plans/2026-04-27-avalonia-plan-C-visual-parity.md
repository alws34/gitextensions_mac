# Avalonia Mac Port — Plan C: Visual Parity

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Mac port look like the original GitExtensions — colored file status icons, a Tree tab in commit details, toolbar visual polish, and a working settings page for the git executable path.

**Architecture:** File status icons are colored TextBlocks styled per status code. The Tree tab uses `git ls-tree -r --name-only` to show the file tree at the selected commit. Toolbar buttons get uniform sizing and Unicode icons. Settings pages already exist; this task verifies they persist correctly.

**Tech Stack:** Avalonia AXAML styles, AvaloniaEdit, C# async/await

---

## File Map

| Action | File |
|---|---|
| Modify | `src/app/GitUI.Avalonia/CommitDetails/FileStatusList.axaml` |
| Modify | `src/app/GitUI.Avalonia/CommitDetails/FileTreeNode.cs` |
| Modify | `src/app/GitUI.Avalonia/CommitDetails/CommitDetailsPanel.axaml` |
| Modify | `src/app/GitUI.Avalonia/CommitDetails/CommitDetailsPanel.axaml.cs` |
| Modify | `src/app/GitUI.Avalonia/AppTheme.axaml` |
| Modify | `src/app/GitUI.Avalonia/MainWindow.axaml.cs` |
| Modify | `src/app/GitUI.Avalonia/Settings/Pages/GitPage.axaml` |
| Modify | `src/app/GitUI.Avalonia/Settings/Pages/GitPage.axaml.cs` |

---

## Task 1: Colored file status icons in FileStatusList

**Files:**
- Modify: `src/app/GitUI.Avalonia/CommitDetails/FileStatusList.axaml`
- Modify: `src/app/GitUI.Avalonia/CommitDetails/FileTreeNode.cs`
- Modify: `src/app/GitUI.Avalonia/AppTheme.axaml`

Currently `DisplayIcon` returns the raw status char. We want colored icons: M=orange, A=green, D=red, R=purple, U=yellow.

- [ ] **Step 1: Write failing test**

Add to `tests/app/GitUI.Avalonia.Tests/CommitDetails/FileTreeNodeTests.cs`:

```csharp
[Test]
public void DisplayIcon_ModifiedFile_ReturnsM()
{
    var item = new FileStatusItem("foo.cs", StagedStatus.Index, statusCode: 'M');
    var node = new FileTreeNode(item);
    Assert.That(node.DisplayIcon, Is.EqualTo("M"));
}
```

NOTE: `FileStatusItem` constructor signature — check `src/app/GitUI.Avalonia/CommitDetails/FileStatusList.axaml.cs` for how `FileStatusItem` is defined. The test should match the actual type. If `FileStatusItem` doesn't expose a status char, skip the unit test and just verify via build.

- [ ] **Step 2: Add `StatusColor` property to `FileTreeNode`**

Read `src/app/GitUI.Avalonia/CommitDetails/FileTreeNode.cs` first, then add a `StatusColor` property:

```csharp
public string StatusColor => FileItem?.Staged switch
{
    StagedStatus.Index    => FileItem?.StatusCode switch
    {
        'A' => "#FF22AA22",
        'D' => "#FFCC2222",
        'R' => "#FF8822CC",
        'U' => "#FFCCAA00",
        _   => "#FFCC6600",   // M and default = orange
    },
    StagedStatus.WorkTree => "#FFCC6600",
    _                     => "#FF808080",
};
```

If `FileStatusItem` doesn't have a `StatusCode` property, check what property holds the git status letter (could be `Status`, `ChangeType`, or similar). Read `FileStatusList.axaml.cs` to find the `FileStatusItem` record definition and adjust accordingly.

If `FileStatusItem` has no status code at all, use a simpler color:
```csharp
public string StatusColor => FileItem is null ? "#FF808080"
    : FileItem.Staged == StagedStatus.Index   ? "#FF22AA22"
    : "#FFCC6600";
```

- [ ] **Step 3: Add status brush resources to AppTheme.axaml**

Read `src/app/GitUI.Avalonia/AppTheme.axaml`, then add after the Ref label colors section:

```xml
<!-- File status icon colors -->
<SolidColorBrush x:Key="FileStatusAdded"    Color="#FF22AA22" />
<SolidColorBrush x:Key="FileStatusModified" Color="#FFCC6600" />
<SolidColorBrush x:Key="FileStatusDeleted"  Color="#FFCC2222" />
<SolidColorBrush x:Key="FileStatusRenamed"  Color="#FF8822CC" />
<SolidColorBrush x:Key="FileStatusUnmerged" Color="#FFCCAA00" />
```

- [ ] **Step 4: Update FileStatusList.axaml to use StatusColor**

In the `TreeDataTemplate`, replace the `DisplayIcon` TextBlock with a colored version:

```xml
<TreeDataTemplate ItemsSource="{Binding Children}">
    <StackPanel Orientation="Horizontal" Spacing="4">
        <TextBlock Text="{Binding DisplayIcon}"
                   Width="14"
                   Foreground="{Binding StatusColor}"
                   FontFamily="Cascadia Code,Menlo,Consolas,monospace"
                   FontWeight="Bold"
                   VerticalAlignment="Center" />
        <TextBlock Text="{Binding Name}"
                   FontFamily="Cascadia Code,Menlo,Consolas,monospace"
                   FontSize="12"
                   VerticalAlignment="Center" />
    </StackPanel>
</TreeDataTemplate>
```

- [ ] **Step 5: Build + run all tests**

```bash
cd /Users/alon/Desktop/gitextensions
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS"
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj 2>&1 | grep -E "Passed|Failed"
```

- [ ] **Step 6: Commit**

```bash
git add src/app/GitUI.Avalonia/CommitDetails/FileStatusList.axaml \
        src/app/GitUI.Avalonia/CommitDetails/FileTreeNode.cs \
        src/app/GitUI.Avalonia/AppTheme.axaml
git commit -m "feat(avalonia): colored file status icons (M=orange, A=green, D=red, R=purple)"
```

---

## Task 2: CommitDetailsPanel — add Tree tab

**Files:**
- Modify: `src/app/GitUI.Avalonia/CommitDetails/CommitDetailsPanel.axaml`
- Modify: `src/app/GitUI.Avalonia/CommitDetails/CommitDetailsPanel.axaml.cs`

Add a third tab "Tree" that shows all files at the selected commit revision using `git ls-tree -r --name-only {hash}`.

- [ ] **Step 1: Add Tree tab to CommitDetailsPanel.axaml**

Read the current file, then add a third `TabItem` after the Files tab:

```xml
<TabItem Header="Tree">
    <TreeView x:Name="CommitTreeView">
        <TreeView.ItemTemplate>
            <TreeDataTemplate ItemsSource="{Binding Children}">
                <TextBlock Text="{Binding Name}"
                           FontFamily="Cascadia Code,Menlo,Consolas,monospace"
                           FontSize="12" />
            </TreeDataTemplate>
        </TreeView.ItemTemplate>
    </TreeView>
</TabItem>
```

- [ ] **Step 2: Create CommitTreeNode**

Create `src/app/GitUI.Avalonia/CommitDetails/CommitTreeNode.cs`:

```csharp
namespace GitUI.Avalonia.CommitDetails;

public sealed class CommitTreeNode
{
    public string Name { get; }
    public bool IsDirectory { get; }
    public List<CommitTreeNode> Children { get; } = [];

    public CommitTreeNode(string name, bool isDirectory)
    {
        Name = name;
        IsDirectory = isDirectory;
    }

    public static IReadOnlyList<CommitTreeNode> BuildTree(IEnumerable<string> paths)
    {
        var root = new CommitTreeNode("root", true);
        foreach (string path in paths)
        {
            string[] parts = path.Split('/');
            CommitTreeNode current = root;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                CommitTreeNode? dir = current.Children
                    .FirstOrDefault(c => c.IsDirectory && c.Name == parts[i]);
                if (dir is null)
                {
                    dir = new CommitTreeNode(parts[i], isDirectory: true);
                    current.Children.Add(dir);
                }
                current = dir;
            }
            current.Children.Add(new CommitTreeNode(parts[^1], isDirectory: false));
        }
        // Sort: dirs first, then files
        SortChildren(root);
        return root.Children;
    }

    private static void SortChildren(CommitTreeNode node)
    {
        node.Children.Sort((a, b) =>
        {
            if (a.IsDirectory != b.IsDirectory)
                return a.IsDirectory ? -1 : 1;
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });
        foreach (CommitTreeNode child in node.Children.Where(c => c.IsDirectory))
            SortChildren(child);
    }
}
```

- [ ] **Step 3: Write failing test for CommitTreeNode**

Create `tests/app/GitUI.Avalonia.Tests/CommitDetails/CommitTreeNodeTests.cs`:

```csharp
using GitUI.Avalonia.CommitDetails;
using NUnit.Framework;

namespace GitUI.Avalonia.Tests.CommitDetails;

[TestFixture]
public class CommitTreeNodeTests
{
    [Test]
    public void BuildTree_FlatFile_ReturnsSingleNode()
    {
        var nodes = CommitTreeNode.BuildTree(["README.md"]);
        Assert.That(nodes, Has.Count.EqualTo(1));
        Assert.That(nodes[0].Name, Is.EqualTo("README.md"));
        Assert.That(nodes[0].IsDirectory, Is.False);
    }

    [Test]
    public void BuildTree_NestedFile_CreatesDirectory()
    {
        var nodes = CommitTreeNode.BuildTree(["src/Foo.cs"]);
        Assert.That(nodes[0].IsDirectory, Is.True);
        Assert.That(nodes[0].Name, Is.EqualTo("src"));
        Assert.That(nodes[0].Children[0].Name, Is.EqualTo("Foo.cs"));
    }

    [Test]
    public void BuildTree_DirectoriesBeforeFiles()
    {
        var nodes = CommitTreeNode.BuildTree(["b.cs", "src/a.cs"]);
        Assert.That(nodes[0].IsDirectory, Is.True);
        Assert.That(nodes[1].IsDirectory, Is.False);
    }
}
```

- [ ] **Step 4: Run test — expect compile error, then implement, then pass**

```bash
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj \
  --filter "FullyQualifiedName~CommitTreeNodeTests" 2>&1 | grep -E "Passed|Failed|error"
```

- [ ] **Step 5: Load tree in CommitDetailsPanel.axaml.cs**

Read the file, find `DetailsTabs_SelectionChanged`, and add a case for the Tree tab (index 2):

```csharp
private void DetailsTabs_SelectionChanged(object? sender, SelectionChangedEventArgs e)
{
    if (_revision is null)
        return;

    int idx = DetailsTabs.SelectedIndex;
    if (idx == 0 && !_diffLoaded)
    {
        _diffLoaded = true;
        _ = DiffView.ShowDiffAsync(_revision);
    }
    else if (idx == 2)
    {
        _ = LoadCommitTreeAsync(_revision);
    }
}

private async System.Threading.Tasks.Task LoadCommitTreeAsync(GitRevision revision)
{
    if (Module is null)
        return;

    string output = await Module.GitExecutable.GetOutputAsync(
        $"ls-tree -r --name-only {revision.Guid}");

    var paths = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                      .Select(p => p.Trim())
                      .Where(p => p.Length > 0)
                      .ToList();

    var nodes = CommitTreeNode.BuildTree(paths);
    await Dispatcher.UIThread.InvokeAsync(() => CommitTreeView.ItemsSource = nodes);
}
```

Add `using Avalonia.Threading;` if not present.

- [ ] **Step 6: Build + run all tests**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS"
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj 2>&1 | grep -E "Passed|Failed"
```

- [ ] **Step 7: Commit**

```bash
git add src/app/GitUI.Avalonia/CommitDetails/CommitDetailsPanel.axaml \
        src/app/GitUI.Avalonia/CommitDetails/CommitDetailsPanel.axaml.cs \
        src/app/GitUI.Avalonia/CommitDetails/CommitTreeNode.cs \
        tests/app/GitUI.Avalonia.Tests/CommitDetails/CommitTreeNodeTests.cs
git commit -m "feat(avalonia): add Tree tab to commit details showing files at revision"
```

---

## Task 3: Toolbar visual polish

**Files:**
- Modify: `src/app/GitUI.Avalonia/MainWindow.axaml.cs`
- Modify: `src/app/GitUI.Avalonia/AppTheme.axaml`

Replace text labels with Unicode icon buttons sized uniformly to match WinForms toolbar feel.

- [ ] **Step 1: Add toolbar button style to AppTheme.axaml**

Read AppTheme.axaml, then add before the closing `</ResourceDictionary>`:

```xml
<!-- Toolbar button style -->
<Style Selector="Button.ToolBtn">
    <Setter Property="Width"          Value="32" />
    <Setter Property="Height"         Value="28" />
    <Setter Property="Padding"        Value="0" />
    <Setter Property="Margin"         Value="1,0" />
    <Setter Property="FontSize"       Value="14" />
    <Setter Property="Background"     Value="Transparent" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="BorderBrush"    Value="Transparent" />
    <Setter Property="CornerRadius"   Value="3" />
</Style>
<Style Selector="Button.ToolBtn:pointerover">
    <Setter Property="BorderBrush" Value="{DynamicResource SystemBaseMediumColor}" />
    <Setter Property="Background"  Value="{DynamicResource SystemChromeMediumColor}" />
</Style>
```

- [ ] **Step 2: Update `BuildToolBar` and `MakeToolButton` in MainWindow.axaml.cs**

Replace the entire `BuildToolBar()` method with:

```csharp
private void BuildToolBar()
{
    MainToolBar.Children.Add(MakeToolButton("☰", "Toggle left panel (Ctrl+W)", ToggleLeftPanel));
    MainToolBar.Children.Add(MakeToolSeparator());
    MainToolBar.Children.Add(MakeToolButton("⟳", "Refresh (F5)", () => _ = RevisionGrid.RefreshAsync()));
    MainToolBar.Children.Add(MakeToolSeparator());
    MainToolBar.Children.Add(MakeToolButton("✎", "Commit staged changes (Ctrl+Enter)",
        () => _ = ShowCommitDialogAsync()));
    MainToolBar.Children.Add(MakeToolButton("⇅", "Fetch all remotes", () => _ = FetchAsync()));
    MainToolBar.Children.Add(MakeToolButton("⬇", "Pull / merge",
        () => _ = ShowModuleDialogAsync(m => new PullDialog(m))));
    MainToolBar.Children.Add(MakeToolButton("⬆", "Push to remote",
        () => _ = ShowModuleDialogAsync(m => new PushDialog(m))));
    MainToolBar.Children.Add(MakeToolButton("≡", "Stash local changes",
        () => _ = ShowModuleDialogAsync(m => new StashDialog(m))));
    MainToolBar.Children.Add(MakeToolSeparator());

    _branchSelector = new ComboBox
    {
        Width = 180,
        PlaceholderText = "Branch",
        IsVisible = false,
        Margin = new Thickness(2, 0),
        VerticalAlignment = VerticalAlignment.Center,
    };
    _branchSelector.SelectionChanged += OnBranchSelectorChanged;
    MainToolBar.Children.Add(_branchSelector);
}
```

Replace `MakeToolButton` with a version that applies the `ToolBtn` class:

```csharp
private static Button MakeToolButton(string icon, string tooltip, Action onClick)
{
    var btn = new Button
    {
        Content = icon,
        Classes = { "ToolBtn" },
        HorizontalContentAlignment = HorizontalAlignment.Center,
        VerticalContentAlignment = VerticalAlignment.Center,
    };
    ToolTip.SetTip(btn, tooltip);
    btn.Click += (_, _) => onClick();
    return btn;
}
```

Add `using Avalonia.Layout;` at the top if not present.

- [ ] **Step 3: Build**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS"
```

- [ ] **Step 4: Commit**

```bash
git add src/app/GitUI.Avalonia/MainWindow.axaml.cs \
        src/app/GitUI.Avalonia/AppTheme.axaml
git commit -m "feat(avalonia): toolbar icon buttons with hover style and tooltips"
```

---

## Task 4: Settings — git executable path + user name/email

**Files:**
- Modify: `src/app/GitUI.Avalonia/Settings/Pages/GitPage.axaml`
- Modify: `src/app/GitUI.Avalonia/Settings/Pages/GitPage.axaml.cs`

- [ ] **Step 1: Read current GitPage files**

```bash
cat src/app/GitUI.Avalonia/Settings/Pages/GitPage.axaml
cat src/app/GitUI.Avalonia/Settings/Pages/GitPage.axaml.cs
```

- [ ] **Step 2: Ensure GitPage.axaml has git path + user fields**

If the file is missing or incomplete, replace with:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="GitUI.Avalonia.Settings.Pages.GitPage">
    <StackPanel Margin="8" Spacing="12">
        <TextBlock Text="Git Settings" FontWeight="SemiBold" FontSize="14" />

        <StackPanel Spacing="4">
            <TextBlock Text="Git executable path:" />
            <Grid ColumnDefinitions="*,Auto">
                <TextBox x:Name="GitPathBox"
                         Watermark="/usr/bin/git"
                         Grid.Column="0" />
                <Button Content="Browse…"
                        Grid.Column="1"
                        Margin="4,0,0,0"
                        Click="BrowseGit_Click" />
            </Grid>
            <TextBlock x:Name="GitVersionLabel"
                       FontSize="11"
                       Foreground="{DynamicResource SystemBaseMediumColor}" />
        </StackPanel>

        <StackPanel Spacing="4">
            <TextBlock Text="User name:" />
            <TextBox x:Name="UserNameBox" Watermark="Your Name" />
        </StackPanel>

        <StackPanel Spacing="4">
            <TextBlock Text="User email:" />
            <TextBox x:Name="UserEmailBox" Watermark="you@example.com" />
        </StackPanel>
    </StackPanel>
</UserControl>
```

- [ ] **Step 3: Implement GitPage.axaml.cs**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GitUI.Avalonia.Settings;

namespace GitUI.Avalonia.Settings.Pages;

public partial class GitPage : UserControl, ISettingsPage
{
    public GitPage()
    {
        InitializeComponent();
        LoadSettings();
        _ = DetectGitVersionAsync();
    }

    private void LoadSettings()
    {
        GitPathBox.Text  = App.Settings.GetString("gitPath",  string.Empty);
        UserNameBox.Text = App.Settings.GetString("userName", string.Empty);
        UserEmailBox.Text= App.Settings.GetString("userEmail",string.Empty);
    }

    public void SaveSettings()
    {
        App.Settings.SetString("gitPath",   GitPathBox.Text?.Trim()  ?? string.Empty);
        App.Settings.SetString("userName",  UserNameBox.Text?.Trim() ?? string.Empty);
        App.Settings.SetString("userEmail", UserEmailBox.Text?.Trim()  ?? string.Empty);
    }

    private async System.Threading.Tasks.Task DetectGitVersionAsync()
    {
        try
        {
            string git = GitPathBox.Text?.Trim() is { Length: > 0 } p ? p : "git";
            var psi = new System.Diagnostics.ProcessStartInfo(git, "--version")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            string version = proc is null ? string.Empty
                : (await proc.StandardOutput.ReadToEndAsync()).Trim();
            await Dispatcher.UIThread.InvokeAsync(() =>
                GitVersionLabel.Text = string.IsNullOrEmpty(version)
                    ? "git not found"
                    : version);
        }
        catch
        {
            await Dispatcher.UIThread.InvokeAsync(() => GitVersionLabel.Text = "git not found");
        }
    }

    private async void BrowseGit_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions { Title = "Select git executable", AllowMultiple = false });

        if (files is [var file])
        {
            GitPathBox.Text = file.Path.LocalPath;
            _ = DetectGitVersionAsync();
        }
    }
}
```

- [ ] **Step 4: Verify ISettingsPage interface exists**

Read `src/app/GitUI.Avalonia/Settings/ISettingsPage.cs`. It should be:

```csharp
namespace GitUI.Avalonia.Settings;
public interface ISettingsPage
{
    void SaveSettings();
}
```

If it doesn't exist or is different, adjust `GitPage` to match.

- [ ] **Step 5: Build + run all tests**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS"
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj 2>&1 | grep -E "Passed|Failed"
```

- [ ] **Step 6: Commit**

```bash
git add src/app/GitUI.Avalonia/Settings/Pages/GitPage.axaml \
        src/app/GitUI.Avalonia/Settings/Pages/GitPage.axaml.cs
git commit -m "feat(avalonia): settings git page — path, user name, email with git version detection"
```

---

## Task 5: CommitSummaryControl — show ref labels inline

**Files:**
- Modify: `src/app/GitUI.Avalonia/CommitDetails/CommitSummaryControl.axaml`
- Modify: `src/app/GitUI.Avalonia/CommitDetails/CommitSummaryControl.axaml.cs`

Show branch/tag badges next to the commit hash, matching the grid display style.

- [ ] **Step 1: Read both files**

```bash
cat src/app/GitUI.Avalonia/CommitDetails/CommitSummaryControl.axaml
cat src/app/GitUI.Avalonia/CommitDetails/CommitSummaryControl.axaml.cs
```

- [ ] **Step 2: Add Refs row to CommitSummaryControl.axaml**

Add an `ItemsControl` row below the hash line:

```xml
<ItemsControl x:Name="RefLabels" IsVisible="False">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <WrapPanel Orientation="Horizontal" />
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="git:IGitRef"
                      xmlns:git="using:GitExtensions.Extensibility.Git">
            <Border Background="#22000000"
                    CornerRadius="3"
                    Padding="4,1"
                    Margin="0,0,4,2">
                <TextBlock Text="{Binding LocalName}"
                           Foreground="{Binding ., Converter={StaticResource RefTypeBrush}}"
                           FontSize="10"
                           FontWeight="SemiBold" />
            </Border>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

- [ ] **Step 3: Populate RefLabels in CommitSummaryControl.axaml.cs**

In the `ShowRevision` (or equivalent) method that sets `CommitHash`, `CommitMessage`, etc., add:

```csharp
if (revision.Refs is { Count: > 0 } refs)
{
    RefLabels.ItemsSource = refs;
    RefLabels.IsVisible = true;
}
else
{
    RefLabels.IsVisible = false;
}
```

Read the .cs file first to find exactly where the revision display code lives and adjust accordingly.

- [ ] **Step 4: Build + run all tests**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|error CS"
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj 2>&1 | grep -E "Passed|Failed"
```

- [ ] **Step 5: Commit**

```bash
git add src/app/GitUI.Avalonia/CommitDetails/CommitSummaryControl.axaml \
        src/app/GitUI.Avalonia/CommitDetails/CommitSummaryControl.axaml.cs
git commit -m "feat(avalonia): show ref labels (branches/tags) in commit summary panel"
```
