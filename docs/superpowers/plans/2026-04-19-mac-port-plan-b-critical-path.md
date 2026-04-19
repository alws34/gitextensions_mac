# Mac Port — Plan B: Critical Path (Core Shell + Commit Graph + Commit Details)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port the main window (FormBrowse), commit graph (RevisionGrid), and commit details panel so that the app opens a real git repo and shows a working commit history.

**Architecture:** Three sequential tasks on the critical path — Core Shell hosts the layout, Commit Graph fills the central panel, Commit Details Panel shows per-commit info. Each depends on the previous.

**Prerequisite:** Plan A must be complete (GitUI.Avalonia builds, Avalonia infrastructure exists).

**Tech Stack:** Avalonia 11, Avalonia.ReactiveUI, AvaloniaEdit, Avalonia.Media (DrawingContext for graph rendering), LibGit2Sharp (already in GitCommands)

**Reference docs:**
- `docs/mac-port/AGENT-GUIDE.md`
- `docs/mac-port/WINFORMS-TO-AVALONIA.md`
- `docs/mac-port/tasks/TASK-2-core-shell.md`
- `docs/mac-port/tasks/TASK-3-commit-graph.md`

---

## File Map

**Task 2 — Core Shell:**
- `src/app/GitUI.Avalonia/MainWindow.axaml` + `.axaml.cs`
- `src/app/GitUI.Avalonia/Dashboard/DashboardView.axaml` + `.axaml.cs`
- `src/app/GitUI.Avalonia/Dashboard/OpenRepositoryDialog.axaml` + `.axaml.cs`
- `src/app/GitUI.Avalonia/Dashboard/RecentReposSettingsDialog.axaml` + `.axaml.cs`
- `src/app/GitUI.Avalonia/Controls/FilterToolBar.axaml` + `.axaml.cs`
- `src/app/GitUI.Avalonia/Controls/RepoStateVisualiser.axaml` + `.axaml.cs`

**Task 3 — Commit Graph:**
- `src/app/GitUI.Avalonia/RevisionGrid/RevisionGridControl.axaml` + `.axaml.cs`
- `src/app/GitUI.Avalonia/RevisionGrid/RevisionDataGrid.axaml` + `.axaml.cs`
- `src/app/GitUI.Avalonia/RevisionGrid/Graph/GraphRenderer.cs`
- `src/app/GitUI.Avalonia/RevisionGrid/Graph/GraphCache.cs`
- `src/app/GitUI.Avalonia/RevisionGrid/Graph/SegmentRenderer.cs`
- `src/app/GitUI.Avalonia/RevisionGrid/Columns/RevisionGraphColumn.cs`
- `src/app/GitUI.Avalonia/RevisionGrid/Columns/MessageColumn.cs`
- `src/app/GitUI.Avalonia/RevisionGrid/Columns/AuthorNameColumn.cs`
- `src/app/GitUI.Avalonia/RevisionGrid/Columns/DateColumn.cs`
- `src/app/GitUI.Avalonia/RevisionGrid/RevisionFilterDialog.axaml` + `.axaml.cs`

**Task 4 — Commit Details:**
- `src/app/GitUI.Avalonia/CommitDetails/CommitDiffControl.axaml` + `.axaml.cs`
- `src/app/GitUI.Avalonia/CommitDetails/CommitSummaryControl.axaml` + `.axaml.cs`
- `src/app/GitUI.Avalonia/CommitDetails/FileStatusList.axaml` + `.axaml.cs`
- `src/app/GitUI.Avalonia/CommitDetails/BlameControl.axaml` + `.axaml.cs`

---

## Task 2: Core Shell — Main Window

**Read first:** `src/app/GitUI/CommandsDialogs/FormBrowse.cs` and its 4 partial files.

### Step 2.1: Create MainWindow layout

- [ ] **Step 1: Create `MainWindow.axaml`**

```xml
<local:GitExtensionsWindow
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:GitUI.Avalonia.Base"
    xmlns:dashboard="using:GitUI.Avalonia.Dashboard"
    xmlns:controls="using:GitUI.Avalonia.Controls"
    x:Class="GitUI.Avalonia.MainWindow"
    Title="Git Extensions"
    Width="1280" Height="800"
    MinWidth="800" MinHeight="500">

    <DockPanel>
        <!-- Menu bar -->
        <Menu DockPanel.Dock="Top" x:Name="MainMenu" />

        <!-- Toolbar -->
        <ToolBar DockPanel.Dock="Top" x:Name="MainToolBar" />

        <!-- Filter toolbar -->
        <controls:FilterToolBar DockPanel.Dock="Top" x:Name="FilterBar"
                                IsVisible="{Binding HasRepository}" />

        <!-- Status bar -->
        <Border DockPanel.Dock="Bottom" Height="24" Background="{DynamicResource SystemChromeLowColor}">
            <TextBlock x:Name="StatusLabel" VerticalAlignment="Center" Margin="8,0" FontSize="12" />
        </Border>

        <!-- Main content: dashboard or repo view -->
        <Grid>
            <dashboard:DashboardView x:Name="Dashboard"
                                     IsVisible="{Binding !HasRepository}" />
            <Grid x:Name="RepoView"
                  IsVisible="{Binding HasRepository}">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" MinWidth="300" />
                    <ColumnDefinition Width="5" />
                    <ColumnDefinition Width="400" MinWidth="200" />
                </Grid.ColumnDefinitions>
                <!-- Commit graph placeholder (filled by Task 3) -->
                <Border Grid.Column="0" x:Name="GraphPlaceholder"
                        Background="{DynamicResource SystemChromeMediumLowColor}">
                    <TextBlock Text="Commit graph loading..."
                               HorizontalAlignment="Center"
                               VerticalAlignment="Center" />
                </Border>
                <GridSplitter Grid.Column="1" ResizeDirection="Columns" />
                <!-- Commit details placeholder (filled by Task 4) -->
                <Border Grid.Column="2" x:Name="DetailsPlaceholder"
                        Background="{DynamicResource SystemChromeMediumLowColor}">
                    <TextBlock Text="Select a commit"
                               HorizontalAlignment="Center"
                               VerticalAlignment="Center" />
                </Border>
            </Grid>
        </Grid>
    </DockPanel>
</local:GitExtensionsWindow>
```

- [ ] **Step 2: Create `MainWindow.axaml.cs`**

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GitCommands;
using GitUI.Avalonia.Base;
using GitUI.Avalonia.Dashboard;

namespace GitUI.Avalonia;

public partial class MainWindow : GitExtensionsWindow
{
    private GitModule? _module;

    public static readonly StyledProperty<bool> HasRepositoryProperty =
        AvaloniaProperty.Register<MainWindow, bool>(nameof(HasRepository));

    public bool HasRepository
    {
        get => GetValue(HasRepositoryProperty);
        private set => SetValue(HasRepositoryProperty, value);
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        BuildMenu();
        LoadRecentRepositories();
    }

    private void LoadRecentRepositories()
    {
        var recent = App.Settings.GetStringList("recentRepositories");
        Dashboard.RecentRepositories = recent
            .Select(p => new RecentRepo(p))
            .ToList();
        Dashboard.OnOpenRepository += OpenRepository;
    }

    public void OpenRepository(string path)
    {
        if (!Directory.Exists(path))
        {
            StatusLabel.Text = $"Path not found: {path}";
            return;
        }

        _module = new GitModule(path);
        HasRepository = true;
        Title = $"{Path.GetFileName(path)} — Git Extensions";
        StatusLabel.Text = path;

        AddToRecentRepositories(path);
    }

    private void AddToRecentRepositories(string path)
    {
        var recent = App.Settings.GetStringList("recentRepositories").ToList();
        recent.Remove(path);
        recent.Insert(0, path);
        if (recent.Count > 20) recent.RemoveRange(20, recent.Count - 20);
        App.Settings.SetStringList("recentRepositories", recent);
        App.Settings.Save();
    }

    private void BuildMenu()
    {
        MainMenu.Items.Add(BuildFileMenu());
        MainMenu.Items.Add(BuildRepositoryMenu());
        MainMenu.Items.Add(BuildCommandsMenu());
        MainMenu.Items.Add(BuildHelpMenu());
    }

    private MenuItem BuildFileMenu()
    {
        var menu = new MenuItem { Header = "_File" };
        menu.Items.Add(new MenuItem { Header = "_Open Repository...", Command = ReactiveUI.ReactiveCommand.CreateFromTask(OpenRepositoryDialogAsync) });
        menu.Items.Add(new MenuItem { Header = "_Clone Repository..." });
        menu.Items.Add(new MenuItem { Header = "_Init New Repository..." });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "_Settings..." });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "E_xit", Command = ReactiveUI.ReactiveCommand.Create(() => (Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.Shutdown()) });
        return menu;
    }

    private MenuItem BuildRepositoryMenu()
    {
        var menu = new MenuItem { Header = "_Repository" };
        menu.Items.Add(new MenuItem { Header = "_Fetch" });
        menu.Items.Add(new MenuItem { Header = "_Pull" });
        menu.Items.Add(new MenuItem { Header = "P_ush" });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "Manage _Remotes..." });
        return menu;
    }

    private MenuItem BuildCommandsMenu()
    {
        var menu = new MenuItem { Header = "_Commands" };
        menu.Items.Add(new MenuItem { Header = "_Commit..." });
        menu.Items.Add(new MenuItem { Header = "Create _Branch..." });
        menu.Items.Add(new MenuItem { Header = "Checkout _Branch..." });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "_Stash..." });
        return menu;
    }

    private MenuItem BuildHelpMenu()
    {
        var menu = new MenuItem { Header = "_Help" };
        menu.Items.Add(new MenuItem { Header = "_About Git Extensions" });
        return menu;
    }

    private async Task OpenRepositoryDialogAsync()
    {
        var dialog = new OpenRepositoryDialog();
        var path = await dialog.ShowDialog<string?>(this);
        if (path is not null)
        {
            OpenRepository(path);
        }
    }
}
```

- [ ] **Step 3: Update `App.axaml.cs` to use MainWindow**

Replace the temporary blank window with `MainWindow`:
```csharp
desktop.MainWindow = new MainWindow();
```

- [ ] **Step 4: Verify app opens with dashboard**

```bash
dotnet run --project src/app/GitUI.Avalonia/GitUI.Avalonia.csproj
```

Expected: App opens with "Git Extensions" title, menu bar present, dashboard area visible.

- [ ] **Step 5: Commit**

```bash
git add src/app/GitUI.Avalonia/
git commit -m "feat: add MainWindow with layout, menu bar, dashboard placeholder"
```

### Step 2.2: Dashboard view

- [ ] **Step 6: Create `Dashboard/DashboardView.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="GitUI.Avalonia.Dashboard.DashboardView">
    <Grid ColumnDefinitions="300,*" RowDefinitions="Auto,*">
        <TextBlock Grid.Column="0" Grid.Row="0"
                   Text="Recent Repositories" FontWeight="Bold" FontSize="14"
                   Margin="16,16,16,8" />
        <ListBox Grid.Column="0" Grid.Row="1"
                 x:Name="RecentList"
                 SelectionChanged="RecentList_SelectionChanged"
                 Margin="8,0">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <StackPanel Margin="4">
                        <TextBlock Text="{Binding Name}" FontWeight="SemiBold" />
                        <TextBlock Text="{Binding Path}" FontSize="11"
                                   Foreground="{DynamicResource SystemBaseMediumColor}"
                                   TextTrimming="LeadingEllipsis" />
                    </StackPanel>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>
        <StackPanel Grid.Column="1" Grid.Row="0" Grid.RowSpan="2"
                    HorizontalAlignment="Center" VerticalAlignment="Center"
                    Spacing="12">
            <TextBlock Text="Git Extensions" FontSize="28" FontWeight="Light"
                       HorizontalAlignment="Center" />
            <Button Content="Open Repository..." Width="200"
                    Click="OpenButton_Click" />
            <Button Content="Clone Repository..." Width="200" />
            <Button Content="Init New Repository..." Width="200" />
        </StackPanel>
    </Grid>
</UserControl>
```

- [ ] **Step 7: Create `Dashboard/DashboardView.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GitUI.Avalonia.Dashboard;

public record RecentRepo(string Path)
{
    public string Name => System.IO.Path.GetFileName(Path);
}

public partial class DashboardView : UserControl
{
    public event Action<string>? OnOpenRepository;

    private IList<RecentRepo> _recentRepositories = [];

    public IList<RecentRepo> RecentRepositories
    {
        get => _recentRepositories;
        set
        {
            _recentRepositories = value;
            RecentList.ItemsSource = value;
        }
    }

    public DashboardView() => InitializeComponent();

    private void RecentList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (RecentList.SelectedItem is RecentRepo repo)
        {
            OnOpenRepository?.Invoke(repo.Path);
        }
    }

    private async void OpenButton_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenRepositoryDialog();
        var topLevel = TopLevel.GetTopLevel(this)!;
        var path = await dialog.ShowDialog<string?>(topLevel as Avalonia.Controls.Window);
        if (path is not null)
        {
            OnOpenRepository?.Invoke(path);
        }
    }
}
```

- [ ] **Step 8: Create `Dashboard/OpenRepositoryDialog.axaml`**

```xml
<local:GitExtensionsDialog xmlns="https://github.com/avaloniaui"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                            xmlns:local="using:GitUI.Avalonia.Base"
                            x:Class="GitUI.Avalonia.Dashboard.OpenRepositoryDialog"
                            Title="Open Repository"
                            Width="500">
    <StackPanel Margin="16" Spacing="8">
        <TextBlock Text="Repository path:" />
        <Grid ColumnDefinitions="*,Auto">
            <TextBox x:Name="PathBox" Grid.Column="0" Watermark="/path/to/repo" />
            <Button Grid.Column="1" Content="Browse..." Margin="8,0,0,0"
                    Click="Browse_Click" />
        </Grid>
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Spacing="8" Margin="0,8,0,0">
            <Button Content="Cancel" Click="Cancel_Click" />
            <Button Content="Open" IsDefault="True" Click="Open_Click" />
        </StackPanel>
    </StackPanel>
</local:GitExtensionsDialog>
```

- [ ] **Step 9: Create `Dashboard/OpenRepositoryDialog.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dashboard;

public partial class OpenRepositoryDialog : GitExtensionsDialog
{
    public OpenRepositoryDialog() => InitializeComponent();

    private async void Browse_Click(object? sender, RoutedEventArgs e)
    {
        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Repository Folder",
            AllowMultiple = false
        });

        if (result.Count > 0)
        {
            PathBox.Text = result[0].Path.LocalPath;
        }
    }

    private void Open_Click(object? sender, RoutedEventArgs e)
    {
        var path = PathBox.Text?.Trim();
        if (!string.IsNullOrEmpty(path))
        {
            Close(path);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
}
```

- [ ] **Step 10: Verify: open repo flow works end-to-end**

```bash
dotnet run --project src/app/GitUI.Avalonia/GitUI.Avalonia.csproj
```

1. Click "Open Repository..."
2. Browse to any git repo on your Mac
3. Dialog closes, window title updates to repo name
4. Dashboard hides, repo view shows (with placeholders)

- [ ] **Step 11: Commit**

```bash
git add src/app/GitUI.Avalonia/Dashboard/ src/app/GitUI.Avalonia/App.axaml.cs
git commit -m "feat: add dashboard, open repository dialog, recent repos list"
```

### Step 2.3: FilterToolBar and RepoStateVisualiser

- [ ] **Step 12: Create `Controls/FilterToolBar.axaml`**

Read source: `src/app/GitUI/UserControls/FilterToolBar.cs`

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="GitUI.Avalonia.Controls.FilterToolBar">
    <DockPanel Height="30">
        <TextBox DockPanel.Dock="Left" Width="200"
                 x:Name="BranchFilter"
                 Watermark="Filter branches..."
                 Margin="4,2" />
        <TextBox DockPanel.Dock="Left" Width="200"
                 x:Name="MessageFilter"
                 Watermark="Filter commits..."
                 Margin="4,2" />
        <Button DockPanel.Dock="Left" Content="×"
                Width="24" Margin="0,2,8,2"
                ToolTip.Tip="Clear filters"
                Click="ClearFilter_Click" />
        <Border />
    </DockPanel>
</UserControl>
```

- [ ] **Step 13: Create `Controls/FilterToolBar.axaml.cs`**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GitUI.Avalonia.Controls;

public partial class FilterToolBar : UserControl
{
    public event Action<string>? BranchFilterChanged;
    public event Action<string>? MessageFilterChanged;

    public FilterToolBar()
    {
        InitializeComponent();
        BranchFilter.TextChanged += (_, _) => BranchFilterChanged?.Invoke(BranchFilter.Text ?? "");
        MessageFilter.TextChanged += (_, _) => MessageFilterChanged?.Invoke(MessageFilter.Text ?? "");
    }

    private void ClearFilter_Click(object? sender, RoutedEventArgs e)
    {
        BranchFilter.Text = "";
        MessageFilter.Text = "";
    }
}
```

- [ ] **Step 14: Commit**

```bash
git add src/app/GitUI.Avalonia/Controls/
git commit -m "feat: add FilterToolBar control"
```

---

## Task 3: Commit Graph

**Read first:** All files listed in `docs/mac-port/tasks/TASK-3-commit-graph.md` under "Source files to read".

The graph layout algorithm files (`Graph/*.cs` except Rendering) need **zero changes** — they are pure data structures. Only copy them into the Avalonia project. The rendering files need to be ported from `System.Drawing` to `Avalonia.Media`.

### Step 3.1: Copy graph data structures unchanged

- [ ] **Step 1: Copy pure data files**

Copy these files from `src/app/GitUI/UserControls/RevisionGrid/Graph/` to `src/app/GitUI.Avalonia/RevisionGrid/Graph/` (no code changes, just copy and update namespace):
- `RevisionGraph.cs`
- `RevisionGraphRevision.cs`
- `RevisionGraphRow.cs`
- `RevisionGraphSegment.cs`
- `RevisionGraphConfig.cs`
- `RevisionGraphLaneColor.cs`
- `Lane.cs`
- `LaneInfo.cs`
- `LaneInfoProvider.cs`
- `LaneNodeLocator.cs`
- `LaneSharing.cs`
- `BranchFinder.cs`

For each file, update the namespace from `GitUI.UserControls.RevisionGrid.Graph` to `GitUI.Avalonia.RevisionGrid.Graph`.

- [ ] **Step 2: Copy non-rendering RevisionGrid support files unchanged**

Copy with namespace update:
- `RevisionGrid/FilterInfo.cs`
- `RevisionGrid/NavigationHistory.cs`
- `RevisionGrid/ParentChildNavigationHistory.cs`
- `RevisionGrid/RevisionFilter.cs`
- `RevisionGrid/VisibleRowRange.cs`
- `RevisionGrid/RevisionLoadEventArgs.cs`
- `RevisionGrid/FilterChangedEventArgs.cs`

- [ ] **Step 3: Commit**

```bash
git add src/app/GitUI.Avalonia/RevisionGrid/
git commit -m "feat: copy graph data structure files (no rendering, zero changes)"
```

### Step 3.2: Port the graph renderer

- [ ] **Step 4: Read the original `GraphRenderer.cs`**

Open `src/app/GitUI/UserControls/RevisionGrid/Graph/Rendering/GraphRenderer.cs` and understand its `Render(Graphics g, ...)` method signature and what it draws.

- [ ] **Step 5: Create `RevisionGrid/Graph/GraphRenderer.cs`**

Port `GraphRenderer.cs`, replacing `System.Drawing` with `Avalonia.Media`:

```csharp
using Avalonia;
using Avalonia.Media;
using GitUI.Avalonia.RevisionGrid.Graph;

namespace GitUI.Avalonia.RevisionGrid.Graph;

/// <summary>
/// Renders commit graph lane segments using Avalonia DrawingContext.
/// Ported from WinForms System.Drawing.Graphics version.
/// </summary>
internal static class GraphRenderer
{
    private const int NodeRadius = 4;
    private const double LaneWidth = 16.0;
    private const double RowHeight = 24.0;

    /// <summary>
    /// Renders a single row's graph segment into the given DrawingContext.
    /// </summary>
    public static void RenderGraphCell(
        DrawingContext ctx,
        RevisionGraphRow row,
        int laneCount,
        double cellWidth,
        double cellHeight,
        IReadOnlyList<Color> laneColors)
    {
        double midY = cellHeight / 2.0;

        // Draw lane segments (lines connecting commits)
        foreach (var segment in row.Segments)
        {
            if (segment.LaneIndex >= laneColors.Count) continue;
            var color = laneColors[segment.LaneIndex];
            var pen = new Pen(new SolidColorBrush(color), 2.0);
            double x = LaneCenterX(segment.LaneIndex);

            // Straight segment
            ctx.DrawLine(pen, new Point(x, 0), new Point(x, cellHeight));
        }

        // Draw commit node (circle) at the node's lane
        if (row.NodeLane >= 0 && row.NodeLane < laneColors.Count)
        {
            var nodeColor = laneColors[row.NodeLane];
            double nodeX = LaneCenterX(row.NodeLane);
            ctx.DrawEllipse(
                new SolidColorBrush(nodeColor),
                new Pen(Brushes.White, 1.0),
                new Point(nodeX, midY),
                NodeRadius, NodeRadius);
        }
    }

    private static double LaneCenterX(int laneIndex)
        => (laneIndex + 0.5) * LaneWidth;
}
```

> **Note:** The real implementation will need to handle diagonal merging segments, merges, branch-offs. Expand this once you have studied the original `SegmentRenderer.cs` and `GraphRenderer.cs` in detail. This skeleton is enough to get the build passing — iterate on visual accuracy.

- [ ] **Step 6: Create `RevisionGrid/Graph/GraphCache.cs`**

```csharp
using Avalonia;
using Avalonia.Media.Imaging;

namespace GitUI.Avalonia.RevisionGrid.Graph;

/// <summary>
/// Caches rendered graph rows as bitmaps for performance.
/// Replaces the WinForms Bitmap-based cache.
/// </summary>
internal class GraphCache : IDisposable
{
    private readonly Dictionary<int, RenderTargetBitmap> _cache = new();
    private readonly int _cellWidth;
    private readonly int _cellHeight;

    public GraphCache(int cellWidth, int cellHeight)
    {
        _cellWidth = cellWidth;
        _cellHeight = cellHeight;
    }

    public bool TryGet(int rowIndex, out RenderTargetBitmap? bitmap)
        => _cache.TryGetValue(rowIndex, out bitmap);

    public void Store(int rowIndex, RenderTargetBitmap bitmap)
    {
        if (_cache.TryGetValue(rowIndex, out var old))
        {
            old.Dispose();
        }
        _cache[rowIndex] = bitmap;
    }

    public void Invalidate() => _cache.Clear();

    public void Dispose()
    {
        foreach (var bmp in _cache.Values) bmp.Dispose();
        _cache.Clear();
    }
}
```

- [ ] **Step 7: Commit**

```bash
git add src/app/GitUI.Avalonia/RevisionGrid/Graph/
git commit -m "feat: port graph renderer and cache to Avalonia.Media"
```

### Step 3.3: RevisionDataGrid — the commit list

- [ ] **Step 8: Create `RevisionGrid/RevisionDataGrid.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="GitUI.Avalonia.RevisionGrid.RevisionDataGrid">
    <ListBox x:Name="CommitList"
             SelectionMode="Single"
             SelectionChanged="CommitList_SelectionChanged"
             VirtualizationMode="Simple">
        <ListBox.ItemTemplate>
            <DataTemplate>
                <Grid ColumnDefinitions="120,*,150,100">
                    <!-- Graph column: custom-drawn -->
                    <Canvas Grid.Column="0" x:Name="GraphCanvas"
                            Width="120" Height="24" />
                    <!-- Message + ref labels -->
                    <TextBlock Grid.Column="1"
                               Text="{Binding Subject}"
                               VerticalAlignment="Center"
                               Margin="4,0" TextTrimming="CharacterEllipsis" />
                    <!-- Author -->
                    <TextBlock Grid.Column="2"
                               Text="{Binding AuthorName}"
                               VerticalAlignment="Center"
                               Margin="4,0" FontSize="11" />
                    <!-- Date -->
                    <TextBlock Grid.Column="3"
                               Text="{Binding AuthorDate, StringFormat='{}{0:yyyy-MM-dd}'}"
                               VerticalAlignment="Center"
                               Margin="4,0" FontSize="11" />
                </Grid>
            </DataTemplate>
        </ListBox.ItemTemplate>
    </ListBox>
</UserControl>
```

- [ ] **Step 9: Create `RevisionGrid/RevisionDataGrid.axaml.cs`**

```csharp
using Avalonia.Controls;
using GitCommands;
using GitExtensions.Extensibility.Git;

namespace GitUI.Avalonia.RevisionGrid;

public partial class RevisionDataGrid : UserControl
{
    public event Action<GitRevision?>? SelectedRevisionChanged;

    public RevisionDataGrid() => InitializeComponent();

    public void LoadRevisions(IReadOnlyList<GitRevision> revisions)
    {
        CommitList.ItemsSource = revisions;
    }

    private void CommitList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectedRevisionChanged?.Invoke(CommitList.SelectedItem as GitRevision);
    }
}
```

- [ ] **Step 10: Commit**

```bash
git add src/app/GitUI.Avalonia/RevisionGrid/RevisionDataGrid.axaml \
        src/app/GitUI.Avalonia/RevisionGrid/RevisionDataGrid.axaml.cs
git commit -m "feat: add RevisionDataGrid with virtualized commit list"
```

### Step 3.4: RevisionGridControl — wires everything together

- [ ] **Step 11: Create `RevisionGrid/RevisionGridControl.axaml`**

```xml
<local:GitModuleControl xmlns="https://github.com/avaloniaui"
                         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                         xmlns:local="using:GitUI.Avalonia.Base"
                         xmlns:grid="using:GitUI.Avalonia.RevisionGrid"
                         x:Class="GitUI.Avalonia.RevisionGrid.RevisionGridControl">
    <DockPanel>
        <grid:RevisionDataGrid x:Name="DataGrid" />
    </DockPanel>
</local:GitModuleControl>
```

- [ ] **Step 12: Create `RevisionGrid/RevisionGridControl.axaml.cs`**

```csharp
using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.RevisionGrid;

public partial class RevisionGridControl : GitModuleControl
{
    public event Action<GitRevision?>? SelectedRevisionChanged;

    public RevisionGridControl()
    {
        InitializeComponent();
        DataGrid.SelectedRevisionChanged += rev => SelectedRevisionChanged?.Invoke(rev);
    }

    protected override void OnModuleSet()
    {
        _ = LoadRevisionsAsync();
    }

    private async Task LoadRevisionsAsync()
    {
        if (Module is null) return;

        var revisions = await Task.Run(() =>
            Module.GetRevisions(limit: 1000).ToList());

        await Dispatcher.UIThread.InvokeAsync(() =>
            DataGrid.LoadRevisions(revisions));
    }
}
```

> **Note:** `Module.GetRevisions()` — check the actual method name in `GitCommands`. It may be `GetAllRevisions()`, `GetLog()`, or accessed via `Module.GitExecutable`. Look at how `RevisionGridControl.cs` loads revisions in the WinForms version and replicate the same call.

- [ ] **Step 13: Wire RevisionGridControl into MainWindow**

In `MainWindow.axaml`, replace the `GraphPlaceholder` Border with:
```xml
<grid:RevisionGridControl Grid.Column="0" x:Name="RevisionGrid"
                           xmlns:grid="using:GitUI.Avalonia.RevisionGrid" />
```

In `MainWindow.axaml.cs`, after `_module = new GitModule(path)`:
```csharp
RevisionGrid.Module = _module;
RevisionGrid.SelectedRevisionChanged += OnRevisionSelected;
```

Add handler:
```csharp
private void OnRevisionSelected(GitRevision? revision)
{
    // Task 4 will hook up the details panel here
    StatusLabel.Text = revision?.ObjectId.ToShortString() ?? "";
}
```

- [ ] **Step 14: Verify commit list loads**

```bash
dotnet run --project src/app/GitUI.Avalonia/GitUI.Avalonia.csproj
```

1. Open a repository with a real git history
2. Verify commits appear in the list (message, author, date)
3. Verify selecting a commit updates the status bar

- [ ] **Step 15: Commit**

```bash
git add src/app/GitUI.Avalonia/RevisionGrid/ src/app/GitUI.Avalonia/MainWindow.axaml \
        src/app/GitUI.Avalonia/MainWindow.axaml.cs
git commit -m "feat: add RevisionGridControl, commit list loads from real git repo"
```

---

## Task 4: Commit Details Panel

**Read first:** `src/app/GitUI/UserControls/CommitDiff.cs`, `CommitSummaryUserControl.cs`, `FileStatusList.cs`

### Step 4.1: CommitSummaryControl

- [ ] **Step 1: Create `CommitDetails/CommitSummaryControl.axaml`**

```xml
<local:GitModuleControl xmlns="https://github.com/avaloniaui"
                         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                         xmlns:local="using:GitUI.Avalonia.Base"
                         x:Class="GitUI.Avalonia.CommitDetails.CommitSummaryControl">
    <StackPanel Margin="8" Spacing="4">
        <TextBlock x:Name="CommitHash" FontFamily="{DynamicResource MonospaceFont}"
                   FontSize="11" Foreground="{DynamicResource SystemBaseMediumColor}" />
        <TextBlock x:Name="CommitMessage" FontWeight="SemiBold" TextWrapping="Wrap" />
        <StackPanel Orientation="Horizontal" Spacing="8">
            <TextBlock x:Name="AuthorName" />
            <TextBlock x:Name="AuthorDate"
                       Foreground="{DynamicResource SystemBaseMediumColor}" FontSize="11" />
        </StackPanel>
    </StackPanel>
</local:GitModuleControl>
```

- [ ] **Step 2: Create `CommitDetails/CommitSummaryControl.axaml.cs`**

```csharp
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.CommitDetails;

public partial class CommitSummaryControl : GitModuleControl
{
    public CommitSummaryControl() => InitializeComponent();

    public void ShowRevision(GitRevision? revision)
    {
        if (revision is null)
        {
            CommitHash.Text = "";
            CommitMessage.Text = "";
            AuthorName.Text = "";
            AuthorDate.Text = "";
            return;
        }

        CommitHash.Text = revision.Guid;
        CommitMessage.Text = revision.Subject;
        AuthorName.Text = revision.Author;
        AuthorDate.Text = revision.AuthorDate.ToString("yyyy-MM-dd HH:mm");
    }
}
```

### Step 4.2: FileStatusList

- [ ] **Step 3: Create `CommitDetails/FileStatusList.axaml`**

```xml
<local:GitModuleControl xmlns="https://github.com/avaloniaui"
                         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                         xmlns:local="using:GitUI.Avalonia.Base"
                         x:Class="GitUI.Avalonia.CommitDetails.FileStatusList">
    <ListBox x:Name="FileList"
             SelectionChanged="FileList_SelectionChanged">
        <ListBox.ItemTemplate>
            <DataTemplate>
                <StackPanel Orientation="Horizontal" Spacing="4">
                    <TextBlock Text="{Binding StatusIcon}" Width="16"
                               FontFamily="{DynamicResource MonospaceFont}" />
                    <TextBlock Text="{Binding Name}"
                               FontFamily="{DynamicResource MonospaceFont}"
                               FontSize="12" />
                </StackPanel>
            </DataTemplate>
        </ListBox.ItemTemplate>
    </ListBox>
</local:GitModuleControl>
```

- [ ] **Step 4: Create `CommitDetails/FileStatusList.axaml.cs`**

```csharp
using Avalonia.Controls;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.CommitDetails;

public record FileStatusItem(string Name, GitChangeType ChangeType)
{
    public string StatusIcon => ChangeType switch
    {
        GitChangeType.Added => "A",
        GitChangeType.Deleted => "D",
        GitChangeType.Modified => "M",
        GitChangeType.Renamed => "R",
        GitChangeType.Copied => "C",
        _ => "?"
    };
}

public partial class FileStatusList : GitModuleControl
{
    public event Action<FileStatusItem?>? SelectedFileChanged;

    public FileStatusList() => InitializeComponent();

    public void LoadFiles(IEnumerable<FileStatusItem> files)
    {
        FileList.ItemsSource = files.ToList();
    }

    private void FileList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectedFileChanged?.Invoke(FileList.SelectedItem as FileStatusItem);
    }
}
```

### Step 4.3: CommitDiffControl (diff viewer)

- [ ] **Step 5: Create `CommitDetails/CommitDiffControl.axaml`**

```xml
<local:GitModuleControl xmlns="https://github.com/avaloniaui"
                         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                         xmlns:local="using:GitUI.Avalonia.Base"
                         xmlns:ae="clr-namespace:AvaloniaEdit;assembly=AvaloniaEdit"
                         x:Class="GitUI.Avalonia.CommitDetails.CommitDiffControl">
    <ae:TextEditor x:Name="DiffEditor"
                   IsReadOnly="True"
                   FontFamily="{DynamicResource MonospaceFont}"
                   FontSize="12"
                   ShowLineNumbers="True"
                   WordWrap="False" />
</local:GitModuleControl>
```

- [ ] **Step 6: Create `CommitDetails/CommitDiffControl.axaml.cs`**

```csharp
using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.CommitDetails;

public partial class CommitDiffControl : GitModuleControl
{
    public CommitDiffControl() => InitializeComponent();

    public async Task ShowDiffAsync(GitRevision revision, string? filePath = null)
    {
        if (Module is null) return;

        string diff = await Task.Run(() =>
        {
            // Get diff for the revision
            string gitArgs = filePath is null
                ? $"diff-tree --no-commit-id -p {revision.Guid}"
                : $"diff-tree --no-commit-id -p {revision.Guid} -- \"{filePath}\"";
            return Module.GitExecutable.GetOutput(gitArgs);
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            DiffEditor.Text = diff;
            ApplyDiffSyntaxHighlighting();
        });
    }

    private void ApplyDiffSyntaxHighlighting()
    {
        // AvaloniaEdit supports syntax highlighting via IHighlightingDefinition.
        // For now use plain text — add diff highlighting in a follow-up.
    }
}
```

### Step 4.4: Wire commit details into MainWindow

- [ ] **Step 7: Create `CommitDetails/CommitDetailsPanel.axaml`**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:details="using:GitUI.Avalonia.CommitDetails"
             x:Class="GitUI.Avalonia.CommitDetails.CommitDetailsPanel">
    <Grid RowDefinitions="Auto,*,5,200">
        <details:CommitSummaryControl x:Name="Summary" Grid.Row="0" />
        <details:CommitDiffControl x:Name="DiffView" Grid.Row="1" />
        <GridSplitter Grid.Row="2" ResizeDirection="Rows" />
        <details:FileStatusList x:Name="FileList" Grid.Row="3" />
    </Grid>
</UserControl>
```

- [ ] **Step 8: Create `CommitDetails/CommitDetailsPanel.axaml.cs`**

```csharp
using Avalonia.Controls;
using GitCommands;
using GitExtensions.Extensibility.Git;

namespace GitUI.Avalonia.CommitDetails;

public partial class CommitDetailsPanel : UserControl
{
    private GitModule? _module;

    public CommitDetailsPanel() => InitializeComponent();

    public void SetModule(GitModule module)
    {
        _module = module;
        Summary.Module = module;
        DiffView.Module = module;
        FileList.Module = module;
        FileList.SelectedFileChanged += OnFileSelected;
    }

    public async Task ShowRevisionAsync(GitRevision? revision)
    {
        Summary.ShowRevision(revision);

        if (revision is null || _module is null) return;

        // Load file list
        var files = await Task.Run(() =>
            _module.GetFilesChanged(revision.Guid)
                   .Select(f => new FileStatusItem(f.Name, f.ChangeType))
                   .ToList());

        FileList.LoadFiles(files);
        await DiffView.ShowDiffAsync(revision);
    }

    private async void OnFileSelected(FileStatusItem? file)
    {
        if (file is null || DiffView.Module is null) return;
        // Show diff for the selected file.
        // The CommitDetailsPanel tracks the current revision — add a field for it:
        // private GitRevision? _currentRevision;  (set in ShowRevisionAsync)
        // Then call: await DiffView.ShowDiffAsync(_currentRevision, file.Name);
    }
}
```

> **Note:** `_module.GetFilesChanged(sha)` — check the actual method name. In WinForms it may be `GetStatusChangedFiles(sha)` or `GetDiffFiles(sha)`. Look at `FormCommit.cs` or `CommitDiff.cs` for how they load the file list for a commit.

- [ ] **Step 9: Wire CommitDetailsPanel into MainWindow**

In `MainWindow.axaml`, replace `DetailsPlaceholder` with:
```xml
<details:CommitDetailsPanel Grid.Column="2" x:Name="DetailsPanel"
                             xmlns:details="using:GitUI.Avalonia.CommitDetails" />
```

In `MainWindow.axaml.cs`, inside `OpenRepository`:
```csharp
DetailsPanel.SetModule(_module);
```

In `OnRevisionSelected`:
```csharp
private void OnRevisionSelected(GitRevision? revision)
{
    _ = DetailsPanel.ShowRevisionAsync(revision);
}
```

- [ ] **Step 10: Verify end-to-end flow**

```bash
dotnet run --project src/app/GitUI.Avalonia/GitUI.Avalonia.csproj
```

1. Open a real git repo
2. Commit list loads
3. Click a commit → summary shows (hash, message, author, date)
4. File list shows changed files
5. Diff text shows in the editor pane

- [ ] **Step 11: Commit Plan B complete**

```bash
git add src/app/GitUI.Avalonia/CommitDetails/ src/app/GitUI.Avalonia/MainWindow.axaml \
        src/app/GitUI.Avalonia/MainWindow.axaml.cs
git commit -m "feat: add commit details panel (summary, file list, diff viewer) — Plan B complete"
```

---

## Plan B Done Criteria

- [ ] App opens and shows dashboard with recent repos
- [ ] Can open any git repo via File > Open or dashboard
- [ ] Commit list loads and shows real commits from the repo
- [ ] Selecting a commit shows: hash, message, author, date, file list, diff text
- [ ] `dotnet build GitExtensions.Mac.slnx` — 0 errors
- [ ] `docs/mac-port/TASK-STATUS.md` — Tasks 2, 3, 4 marked `complete`

Plan C (`2026-04-19-mac-port-plan-c-screen-wave-1.md`) covers dialogs for commit, branches, remotes, tags, settings, and submodules — all parallelizable.
