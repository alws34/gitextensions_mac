# Avalonia Mac Port Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix three categories of bugs in the GitExtensions Avalonia Mac port — segmented graph lines, bad UI styling, and crashes on button press — so the app feels like original GitExtensions.

**Architecture:** Three independent fixes: (1) change GraphRenderer's per-row line drawing to use a midpoint-boundary formula that guarantees adjacent row endpoints always align; (2) add Avalonia styles for alternating row colours, hover, and selection plus column headers; (3) fix null-forgiving dereference in MainWindow and add a global exception guard in App.

**Tech Stack:** Avalonia UI 11, C# 12, NUnit 3, `~/.dotnet/dotnet` (dotnet binary path on this machine)

---

## File Map

| File | Change |
|------|--------|
| `src/app/GitUI.Avalonia/RevisionGrid/Graph/GraphRenderer.cs` | Add `internal BoundaryX()`, change upper/lower curve endpoints |
| `src/app/GitUI.Avalonia/RevisionGrid/RevisionRow.cs` | Expose `Refs` property |
| `src/app/GitUI.Avalonia/RevisionGrid/RevisionDataGrid.axaml` | Add column headers, styles, ref-label badges, row height 26px |
| `src/app/GitUI.Avalonia/App.axaml` | Add global ListBox row styles |
| `src/app/GitUI.Avalonia/App.axaml.cs` | Add `TaskScheduler.UnobservedTaskException` + `AppDomain.UnhandledException` handlers |
| `src/app/GitUI.Avalonia/MainWindow.axaml.cs` | Fix `_module!` → `ShowModuleDialogAsync`; refresh grid + show status after Fetch |
| `src/app/GitUI.Avalonia/RevisionGrid/RevisionGridControl.axaml.cs` | Expose public `RefreshAsync()` |
| `tests/app/GitUI.Avalonia.Tests/RevisionGrid/Graph/GraphRendererTests.cs` | New — NUnit tests for `BoundaryX` |

---

## Task 1: Fix segmented graph lines — expose `BoundaryX` and add tests

The root cause: for pass-through segments when a lane changes from A to B, the current code draws the lower half of Row N ending at `LaneCenterX(endIdx)` (= B) while Row N+1's upper half starts at `LaneCenterX(startIdx)` (= A). Since A ≠ B the two rows' endpoints don't touch.

Fix: compute the boundary x as the midpoint `(LaneCenterX(thisCenter) + LaneCenterX(adjacent)) / 2`. Both adjacent rows compute the same midpoint from their own data, so endpoints always match.

**Files:**
- Modify: `src/app/GitUI.Avalonia/RevisionGrid/Graph/GraphRenderer.cs`
- Create: `tests/app/GitUI.Avalonia.Tests/RevisionGrid/Graph/GraphRendererTests.cs`

- [ ] **Step 1: Create the test file**

```csharp
// tests/app/GitUI.Avalonia.Tests/RevisionGrid/Graph/GraphRendererTests.cs
using GitUI.Avalonia.RevisionGrid.Graph;
using NUnit.Framework;

namespace GitUI.Avalonia.Tests.RevisionGrid.Graph;

[TestFixture]
public class GraphRendererTests
{
    [Test]
    public void BoundaryX_SameLane_ReturnsCenterOfThatLane()
    {
        double expected = GraphRenderer.LaneCenterX(0);
        Assert.That(GraphRenderer.BoundaryX(0, 0), Is.EqualTo(expected));
    }

    [Test]
    public void BoundaryX_AdjacentLanes_ReturnsMidpoint()
    {
        double lane0 = GraphRenderer.LaneCenterX(0);
        double lane1 = GraphRenderer.LaneCenterX(1);
        double expected = (lane0 + lane1) / 2.0;
        Assert.That(GraphRenderer.BoundaryX(0, 1), Is.EqualTo(expected));
    }

    [Test]
    public void BoundaryX_IsSymmetric()
    {
        Assert.That(GraphRenderer.BoundaryX(0, 2), Is.EqualTo(GraphRenderer.BoundaryX(2, 0)));
    }

    [Test]
    public void BoundaryX_RowNAndRowNPlusOne_ProduceSameValue()
    {
        // Segment in lane 1 in Row N, moving to lane 3 in Row N+1.
        // Row N   lower boundary  = BoundaryX(centerN=1, endIdx=3)
        // Row N+1 upper boundary  = BoundaryX(centerN1=3, startIdx=1)
        // Both must be equal.
        double rowNBottom   = GraphRenderer.BoundaryX(1, 3);
        double rowN1Top     = GraphRenderer.BoundaryX(3, 1);
        Assert.That(rowNBottom, Is.EqualTo(rowN1Top));
    }
}
```

- [ ] **Step 2: Run tests — expect compile error because BoundaryX and LaneCenterX are private**

```bash
cd /Users/alon/Desktop/gitextensions
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj \
  --filter "FullyQualifiedName~GraphRendererTests" 2>&1 | tail -15
```

Expected: build error `GraphRenderer does not contain a definition for 'BoundaryX'`

- [ ] **Step 3: Update GraphRenderer.cs — make LaneCenterX and BoundaryX internal, change curve drawing**

Replace the entire `GraphRenderer.cs` with:

```csharp
using Avalonia;
using Avalonia.Media;

namespace GitUI.Avalonia.RevisionGrid.Graph;

internal static class GraphRenderer
{
    internal const double LaneWidth = 16.0;
    private const double NodeRadius = 4.5;
    private const double LineWidth = 2.0;
    private const int NoLane = -1;

    /// <summary>
    /// Renders one row of the commit graph. Needs the adjacent rows to compute diagonal connections.
    /// </summary>
    public static void RenderGraphCell(
        DrawingContext ctx,
        IRevisionGraphRow? currentRow,
        IRevisionGraphRow? previousRow,
        IRevisionGraphRow? nextRow,
        double cellWidth,
        double cellHeight)
    {
        if (currentRow is null)
        {
            return;
        }

        double midY = cellHeight / 2.0;

        // Render segments in reverse order so the primary lane draws on top
        foreach (RevisionGraphSegment segment in currentRow.Segments.Reverse())
        {
            Lane currentLane = currentRow.GetLaneForSegment(segment);
            if (currentLane.Index < 0 || currentLane.Index >= RevisionGraph.MaxLanes)
            {
                continue;
            }

            // Skip secondary-shared segments to avoid double-drawing aliasing artifacts
            if (currentLane.Sharing == LaneSharing.Entire)
            {
                continue;
            }

            int centerIdx = currentLane.Index;
            int startIdx = NoLane;
            int endIdx = NoLane;

            if (segment.Parent == currentRow.Revision)
            {
                // Segment terminates at this commit (comes from above)
                startIdx = GetLaneIndex(previousRow, segment);
            }
            else if (segment.Child == currentRow.Revision)
            {
                // Segment originates at this commit (goes down)
                endIdx = GetLaneIndex(nextRow, segment);
            }
            else
            {
                // Segment passes straight through
                startIdx = GetLaneIndex(previousRow, segment);
                endIdx = GetLaneIndex(nextRow, segment);
            }

            IBrush brush = segment.LaneInfo is not null
                ? RevisionGraphLaneColor.GetBrushForLane(segment.LaneInfo.Color)
                : RevisionGraphLaneColor.NonRelativeBrush;
            var pen = new Pen(brush, LineWidth);

            double cx = LaneCenterX(centerIdx);

            // Boundary x-positions shared with adjacent rows.
            // Using the midpoint between this lane and the adjacent lane guarantees that both
            // this row's endpoint and the neighbour's endpoint land on the same pixel — preventing
            // the "segmented" appearance when a segment changes lanes between rows.
            double topX = startIdx >= 0 ? BoundaryX(centerIdx, startIdx) : cx;
            double botX = endIdx >= 0 ? BoundaryX(centerIdx, endIdx) : cx;

            if (startIdx >= 0)
            {
                DrawCurve(ctx, pen, topX, 0, cx, midY);
            }

            if (endIdx >= 0)
            {
                DrawCurve(ctx, pen, cx, midY, botX, cellHeight);
            }
        }

        // Draw the commit node
        int nodeLane = currentRow.GetCurrentRevisionLane();
        if (nodeLane >= 0 && nodeLane < RevisionGraph.MaxLanes)
        {
            RevisionGraphSegment? nodeSegment = currentRow.Segments
                .FirstOrDefault(s => s.Parent == currentRow.Revision || s.Child == currentRow.Revision);

            IBrush nodeBrush = nodeSegment?.LaneInfo is not null
                ? RevisionGraphLaneColor.GetBrushForLane(nodeSegment.LaneInfo.Color)
                : RevisionGraphLaneColor.NonRelativeBrush;

            double nodeX = LaneCenterX(nodeLane);
            bool hasRefs = currentRow.Revision.GitRevision?.Refs.Count > 0;

            if (hasRefs)
            {
                ctx.DrawRectangle(
                    nodeBrush,
                    new Pen(Brushes.White, 1.5),
                    new Rect(nodeX - NodeRadius, midY - NodeRadius, NodeRadius * 2, NodeRadius * 2));
            }
            else
            {
                ctx.DrawEllipse(
                    nodeBrush,
                    new Pen(Brushes.White, 1.5),
                    new Point(nodeX, midY),
                    NodeRadius,
                    NodeRadius);
            }
        }
    }

    /// <summary>
    /// Returns the x-coordinate at the boundary between this row and an adjacent row.
    /// It is the midpoint between the two lanes' centres.  Because both sides of the boundary
    /// use the same formula with the same two lane indices (just swapped), the values are
    /// always equal — guaranteeing visually connected lines across rows.
    /// </summary>
    internal static double BoundaryX(int thisCenterLane, int adjacentLane)
        => (LaneCenterX(thisCenterLane) + LaneCenterX(adjacentLane)) / 2.0;

    internal static double LaneCenterX(int laneIndex) => (laneIndex + 0.5) * LaneWidth;

    private static int GetLaneIndex(IRevisionGraphRow? row, RevisionGraphSegment segment)
    {
        if (row is null)
        {
            return NoLane;
        }

        int idx = row.GetLaneForSegment(segment).Index;
        return idx >= 0 ? idx : NoLane;
    }

    /// <summary>
    /// Draws a straight vertical line or an S-shaped Bezier curve for diagonal lane changes.
    /// </summary>
    private static void DrawCurve(DrawingContext ctx, Pen pen, double x0, double y0, double x1, double y1)
    {
        if (Math.Abs(x0 - x1) < 0.5)
        {
            ctx.DrawLine(pen, new Point(x0, y0), new Point(x1, y1));
            return;
        }

        double midY = (y0 + y1) / 2.0;
        var geometry = new StreamGeometry();
        using var sgc = geometry.Open();
        sgc.BeginFigure(new Point(x0, y0), false);
        sgc.CubicBezierTo(
            new Point(x0, midY),
            new Point(x1, midY),
            new Point(x1, y1));
        sgc.EndFigure(false);
        ctx.DrawGeometry(null, pen, geometry);
    }
}
```

- [ ] **Step 4: Run tests — expect PASS**

```bash
cd /Users/alon/Desktop/gitextensions
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj \
  --filter "FullyQualifiedName~GraphRendererTests" 2>&1 | tail -10
```

Expected output contains: `Passed! - Failed: 0`

- [ ] **Step 5: Build the main project — expect 0 errors**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "error|Error|0 Error" | head -5
```

Expected: `0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add src/app/GitUI.Avalonia/RevisionGrid/Graph/GraphRenderer.cs \
        tests/app/GitUI.Avalonia.Tests/RevisionGrid/Graph/GraphRendererTests.cs
git commit -m "fix(avalonia): connect graph lines at row boundaries using midpoint formula"
```

---

## Task 2: Fix null-crash on Clone menu item

**Files:**
- Modify: `src/app/GitUI.Avalonia/MainWindow.axaml.cs:89`

The line `Command = ReactiveCommand.CreateFromTask(() => ShowDialogAsync(() => new CloneDialog(_module!)))` uses the null-forgiving `!` on `_module`. If the user clicks Clone before opening a repo, `_module` is null and this throws `NullReferenceException`.

Clone needs a module for the target path default but can work without one; the simplest safe fix is to use `ShowModuleDialogAsync` (which already guards for null) and pass a factory that receives the non-null module.

- [ ] **Step 1: In `BuildFileMenu()`, change the Clone menu item**

Find this line in `MainWindow.axaml.cs`:
```csharp
menu.Items.Add(new MenuItem { Header = "_Clone Repository...", Command = ReactiveCommand.CreateFromTask(() => ShowDialogAsync(() => new CloneDialog(_module!))) });
```

Replace with:
```csharp
menu.Items.Add(new MenuItem { Header = "_Clone Repository...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new CloneDialog(m))) });
```

- [ ] **Step 2: Build — expect 0 errors**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|0 Error"
```

Expected: `Build succeeded.` and `0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add src/app/GitUI.Avalonia/MainWindow.axaml.cs
git commit -m "fix(avalonia): remove null-forgiving _module! on Clone menu item"
```

---

## Task 3: Add global exception handler

Unhandled exceptions in fire-and-forget `_ = SomeAsync()` tasks currently crash silently or show a raw crash dialog. Register handlers to surface them as status-bar messages.

**Files:**
- Modify: `src/app/GitUI.Avalonia/App.axaml.cs`

- [ ] **Step 1: Add exception handlers at the end of `OnFrameworkInitializationCompleted`**

In `App.axaml.cs`, after the line `GitUI.ThreadHelper.JoinableTaskContext = new JoinableTaskContext();`, add:

```csharp
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            e.SetObserved();
            System.Diagnostics.Debug.WriteLine($"Unobserved task exception: {e.Exception}");
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (desktop.MainWindow is MainWindow mw)
                {
                    mw.ShowError(e.Exception.GetBaseException().Message);
                }
            });
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            string msg = (e.ExceptionObject as Exception)?.Message ?? e.ExceptionObject?.ToString() ?? "Unknown error";
            System.Diagnostics.Debug.WriteLine($"Unhandled domain exception: {msg}");
        };
```

- [ ] **Step 2: Add `ShowError` method to `MainWindow.axaml.cs`**

Add this method anywhere inside the `MainWindow` class (e.g., after `OnRevisionSelected`):

```csharp
    public void ShowError(string message)
    {
        StatusLabel.Text = $"Error: {message}";
    }
```

- [ ] **Step 3: Build — expect 0 errors**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|0 Error"
```

- [ ] **Step 4: Commit**

```bash
git add src/app/GitUI.Avalonia/App.axaml.cs src/app/GitUI.Avalonia/MainWindow.axaml.cs
git commit -m "fix(avalonia): surface unhandled task exceptions in status bar instead of crashing"
```

---

## Task 4: Expose `RefreshAsync` and refresh after Fetch

After a successful `git fetch`, the revision list should reload to show new commits.

**Files:**
- Modify: `src/app/GitUI.Avalonia/RevisionGrid/RevisionGridControl.axaml.cs`
- Modify: `src/app/GitUI.Avalonia/MainWindow.axaml.cs`

- [ ] **Step 1: Add `RefreshAsync` to `RevisionGridControl`**

In `RevisionGridControl.axaml.cs`, add this public method after `OnModuleSet`:

```csharp
    public Task RefreshAsync() => LoadRevisionsAsync();
```

- [ ] **Step 2: Update `FetchAsync` in `MainWindow.axaml.cs` to refresh and show status**

Find the existing `FetchAsync` method:
```csharp
    private async System.Threading.Tasks.Task FetchAsync()
    {
        if (_module is null)
        {
            return;
        }

        await _module.GitExecutable.GetOutputAsync("fetch --all");
        StatusLabel.Text = "Fetch complete";
    }
```

Replace with:
```csharp
    private async System.Threading.Tasks.Task FetchAsync()
    {
        if (_module is null)
        {
            return;
        }

        StatusLabel.Text = "Fetching…";
        try
        {
            string output = await _module.GitExecutable.GetOutputAsync("fetch --all");
            StatusLabel.Text = string.IsNullOrWhiteSpace(output) ? "Fetch complete" : output.Trim();
            await RevisionGrid.RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }
```

- [ ] **Step 3: Build — expect 0 errors**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|0 Error"
```

- [ ] **Step 4: Commit**

```bash
git add src/app/GitUI.Avalonia/RevisionGrid/RevisionGridControl.axaml.cs \
        src/app/GitUI.Avalonia/MainWindow.axaml.cs
git commit -m "fix(avalonia): refresh revision grid after fetch; show fetch output in status bar"
```

---

## Task 5: UI polish — global ListBox row styles

Add alternating row colours, hover highlight, and selection highlight to every ListBox in the app. These go in `App.axaml` so they apply globally (scoped by FluentTheme's override chain).

**Files:**
- Modify: `src/app/GitUI.Avalonia/App.axaml`

- [ ] **Step 1: Replace the entire `App.axaml` with the styled version**

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="GitUI.Avalonia.App">
    <Application.Styles>
        <FluentTheme />
        <Style Selector="ListBoxItem">
            <Setter Property="Padding" Value="0" />
            <Setter Property="MinHeight" Value="26" />
        </Style>
        <Style Selector="ListBoxItem:nth-child(2n)">
            <Setter Property="Background" Value="#08000000" />
        </Style>
        <Style Selector="ListBoxItem:pointerover /template/ ContentPresenter">
            <Setter Property="Background" Value="#1A0078D4" />
        </Style>
        <Style Selector="ListBoxItem:selected /template/ ContentPresenter">
            <Setter Property="Background" Value="#330078D4" />
        </Style>
        <Style Selector="ListBoxItem:selected:pointerover /template/ ContentPresenter">
            <Setter Property="Background" Value="#440078D4" />
        </Style>
    </Application.Styles>
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source="avares://GitUI.Avalonia/AppTheme.axaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

- [ ] **Step 2: Build — expect 0 errors**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|0 Error"
```

- [ ] **Step 3: Commit**

```bash
git add src/app/GitUI.Avalonia/App.axaml
git commit -m "style(avalonia): add alternating row colours and selection highlight to ListBox"
```

---

## Task 6: UI polish — RevisionDataGrid column headers and row height

Add column headers (Graph | Subject | Author | Date), increase row height to 26px, and expose `Refs` on `RevisionRow` so branch/tag names show up inline.

**Files:**
- Modify: `src/app/GitUI.Avalonia/RevisionGrid/RevisionRow.cs`
- Modify: `src/app/GitUI.Avalonia/RevisionGrid/RevisionDataGrid.axaml`

- [ ] **Step 1: Add `Refs` property to `RevisionRow.cs`**

Replace the entire `RevisionRow.cs`:

```csharp
using GitUI.Avalonia.RevisionGrid.Graph;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.RevisionGrid;

public sealed class RevisionRow
{
    public RevisionRow(
        GitRevision revision,
        IRevisionGraphRow? graphRow,
        IRevisionGraphRow? prevRow,
        IRevisionGraphRow? nextRow)
    {
        Revision = revision;
        GraphRow = graphRow;
        PrevRow = prevRow;
        NextRow = nextRow;
    }

    public GitRevision Revision { get; }
    public IRevisionGraphRow? GraphRow { get; }
    public IRevisionGraphRow? PrevRow { get; }
    public IRevisionGraphRow? NextRow { get; }

    public string Subject => Revision.Subject ?? string.Empty;
    public string Author => Revision.Author ?? string.Empty;
    public DateTimeOffset AuthorDate => DateTimeOffset.FromUnixTimeSeconds(Revision.AuthorUnixTime);

    public IReadOnlyList<IGitRef> Refs => Revision.Refs ?? [];

    public string RefsText => Refs.Count == 0
        ? string.Empty
        : string.Join("  ", Refs.Select(r => r.LocalName));
}
```

- [ ] **Step 2: Replace `RevisionDataGrid.axaml` with styled version including headers and ref labels**

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="clr-namespace:GitUI.Avalonia.RevisionGrid"
             x:Class="GitUI.Avalonia.RevisionGrid.RevisionDataGrid">
    <DockPanel>
        <!-- Column headers -->
        <Grid DockPanel.Dock="Top"
              ColumnDefinitions="120,*,150,100"
              Background="{DynamicResource SystemChromeMediumColor}">
            <TextBlock Grid.Column="0" Text="Graph"
                       FontSize="11" FontWeight="SemiBold"
                       Margin="4,2" VerticalAlignment="Center" />
            <TextBlock Grid.Column="1" Text="Subject"
                       FontSize="11" FontWeight="SemiBold"
                       Margin="4,2" VerticalAlignment="Center" />
            <TextBlock Grid.Column="2" Text="Author"
                       FontSize="11" FontWeight="SemiBold"
                       Margin="4,2" VerticalAlignment="Center" />
            <TextBlock Grid.Column="3" Text="Date"
                       FontSize="11" FontWeight="SemiBold"
                       Margin="4,2" VerticalAlignment="Center" />
        </Grid>

        <ListBox x:Name="CommitList"
                 SelectionMode="Single"
                 SelectionChanged="CommitList_SelectionChanged"
                 ScrollViewer.HorizontalScrollBarVisibility="Disabled">
            <ListBox.ItemTemplate>
                <DataTemplate x:DataType="local:RevisionRow">
                    <Grid ColumnDefinitions="120,*,150,100" Height="26">
                        <local:GraphCell Grid.Column="0"
                                         Row="{Binding GraphRow}"
                                         PrevRow="{Binding PrevRow}"
                                         NextRow="{Binding NextRow}"
                                         Width="120" Height="26" />

                        <!-- Subject with optional ref-label prefix -->
                        <StackPanel Grid.Column="1"
                                    Orientation="Horizontal"
                                    VerticalAlignment="Center"
                                    Spacing="4"
                                    Margin="4,0">
                            <TextBlock Text="{Binding RefsText}"
                                       Foreground="{DynamicResource RefLabelLocalBranch}"
                                       FontSize="10"
                                       FontWeight="SemiBold"
                                       VerticalAlignment="Center"
                                       IsVisible="{Binding RefsText, Converter={x:Static StringConverters.IsNotNullOrEmpty}}" />
                            <TextBlock Text="{Binding Subject}"
                                       VerticalAlignment="Center"
                                       TextTrimming="CharacterEllipsis" />
                        </StackPanel>

                        <TextBlock Grid.Column="2"
                                   Text="{Binding Author}"
                                   VerticalAlignment="Center"
                                   Margin="4,0"
                                   FontSize="11"
                                   TextTrimming="CharacterEllipsis" />
                        <TextBlock Grid.Column="3"
                                   Text="{Binding AuthorDate, StringFormat='{}{0:yyyy-MM-dd}'}"
                                   VerticalAlignment="Center"
                                   Margin="4,0"
                                   FontSize="11" />
                    </Grid>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>
    </DockPanel>
</UserControl>
```

- [ ] **Step 3: Build — expect 0 errors**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | grep -E "^Build|0 Error"
```

- [ ] **Step 4: Commit**

```bash
git add src/app/GitUI.Avalonia/RevisionGrid/RevisionRow.cs \
        src/app/GitUI.Avalonia/RevisionGrid/RevisionDataGrid.axaml
git commit -m "style(avalonia): add column headers, ref labels, and 26px row height to revision grid"
```

---

## Task 7: Self-review verification

Run all Avalonia tests to make sure nothing regressed.

- [ ] **Step 1: Run all Avalonia tests**

```bash
~/.dotnet/dotnet test tests/app/GitUI.Avalonia.Tests/GitUI.Avalonia.Tests.csproj --verbosity normal 2>&1 | tail -20
```

Expected: `Passed! - Failed: 0`

- [ ] **Step 2: Final build check**

```bash
~/.dotnet/dotnet build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj 2>&1 | tail -3
```

Expected: `Build succeeded.`  and `0 Error(s)`

---

## Spec Coverage Check

| Design requirement | Task |
|---|---|
| Fix segmented graph lines (midpoint boundary formula) | Task 1 |
| Fix `_module!` null crash (CloneDialog) | Task 2 |
| Global exception handler | Task 3 |
| Refresh grid after Fetch | Task 4 |
| ListBox alternating rows / hover / selection | Task 5 |
| Column headers | Task 6 |
| Ref label badges | Task 6 |
| Row height 26px | Task 5 + 6 |
