# Task 3 — Commit Graph

**Status:** pending  
**Depends on:** Task 2  
**Blocks:** Task 4 (Commit Details)  
**Estimated complexity:** Very High — custom-drawn control, most complex task in the port

---

## Goal

Port the commit graph (RevisionGrid) to Avalonia. This is the central, defining feature of GitExtensions. The graph must render correctly: commit lanes with colored branches, ref labels (branches, tags, HEAD), commit messages, author, date. Clicking a commit selects it.

---

## Source files to read

### Graph data structures (pure C# — keep as-is, NO changes needed)
```
src/app/GitUI/UserControls/RevisionGrid/Graph/RevisionGraph.cs
src/app/GitUI/UserControls/RevisionGrid/Graph/RevisionGraphRevision.cs
src/app/GitUI/UserControls/RevisionGrid/Graph/RevisionGraphRow.cs
src/app/GitUI/UserControls/RevisionGrid/Graph/RevisionGraphSegment.cs
src/app/GitUI/UserControls/RevisionGrid/Graph/RevisionGraphConfig.cs
src/app/GitUI/UserControls/RevisionGrid/Graph/RevisionGraphLaneColor.cs
src/app/GitUI/UserControls/RevisionGrid/Graph/Lane.cs
src/app/GitUI/UserControls/RevisionGrid/Graph/LaneInfo.cs
src/app/GitUI/UserControls/RevisionGrid/Graph/LaneInfoProvider.cs
src/app/GitUI/UserControls/RevisionGrid/Graph/LaneNodeLocator.cs
src/app/GitUI/UserControls/RevisionGrid/Graph/LaneSharing.cs
src/app/GitUI/UserControls/RevisionGrid/Graph/BranchFinder.cs
```

### Rendering (MUST port — uses System.Drawing)
```
src/app/GitUI/UserControls/RevisionGrid/Graph/Rendering/GraphRenderer.cs
src/app/GitUI/UserControls/RevisionGrid/Graph/Rendering/GraphCache.cs
src/app/GitUI/UserControls/RevisionGrid/Graph/Rendering/SegmentRenderer.cs
src/app/GitUI/UserControls/RevisionGrid/Graph/Rendering/Context.cs
src/app/GitUI/UserControls/RevisionGrid/Graph/Rendering/DiagonalSegmentInfo.cs
src/app/GitUI/UserControls/RevisionGrid/Graph/Rendering/SegmentLanesInfo.cs
src/app/GitUI/UserControls/RevisionGrid/Graph/Rendering/SegmentPointsInfo.cs
```

### Grid control (MUST port — WinForms DataGridView)
```
src/app/GitUI/UserControls/RevisionGrid/RevisionGridControl.cs
src/app/GitUI/UserControls/RevisionGrid/RevisionGridControl.Command.cs
src/app/GitUI/UserControls/RevisionGrid/RevisionDataGridView.cs
src/app/GitUI/UserControls/RevisionGrid/RevisionDataGridView.BackgroundUpdater.cs
src/app/GitUI/UserControls/RevisionGrid/RevisionGridRefRenderer.cs
src/app/GitUI/UserControls/RevisionGrid/RevisionGridToolTipProvider.cs
src/app/GitUI/UserControls/RevisionGrid/RevisionGridMenuCommands.cs
```

### Columns (MUST port)
```
src/app/GitUI/UserControls/RevisionGrid/Columns/RevisionGraphColumnProvider.cs
src/app/GitUI/UserControls/RevisionGrid/Columns/MessageColumnProvider.cs
src/app/GitUI/UserControls/RevisionGrid/Columns/AuthorNameColumnProvider.cs
src/app/GitUI/UserControls/RevisionGrid/Columns/DateColumnProvider.cs
src/app/GitUI/UserControls/RevisionGrid/Columns/CommitIdColumnProvider.cs
src/app/GitUI/UserControls/RevisionGrid/Columns/AvatarColumnProvider.cs
src/app/GitUI/UserControls/RevisionGrid/Columns/BuildStatusColumnProvider.cs
src/app/GitUI/UserControls/RevisionGrid/Columns/ColumnProvider.cs
src/app/GitUI/UserControls/RevisionGrid/Columns/NotesColumnProvider.cs
```

### Filters and context menus (MUST port)
```
src/app/GitUI/UserControls/RevisionGrid/FormRevisionFilter.cs
src/app/GitUI/UserControls/RevisionGrid/FormQuickGitRefSelector.cs
src/app/GitUI/UserControls/RevisionGrid/FormQuickItemSelector.cs
src/app/GitUI/UserControls/RevisionGrid/FormQuickStringSelector.cs
src/app/GitUI/UserControls/RevisionGrid/RefContextMenus/
```

---

## Files to create

```
src/app/GitUI.Avalonia/RevisionGrid/
    RevisionGridControl.axaml
    RevisionGridControl.axaml.cs
    RevisionDataGrid.axaml           (the actual grid/list)
    RevisionDataGrid.axaml.cs
    Graph/
        GraphRenderer.cs             (port from System.Drawing → Avalonia.Media)
        GraphCache.cs                (port)
        SegmentRenderer.cs           (port)
        Context.cs                   (port)
        (all other files: copy unchanged — pure data, no WinForms)
    Columns/
        RevisionGraphColumn.cs       (custom Avalonia column)
        MessageColumn.cs
        AuthorNameColumn.cs
        DateColumn.cs
        CommitIdColumn.cs
        AvatarColumn.cs
    RevisionFilterDialog.axaml
    RevisionFilterDialog.axaml.cs
    QuickSelectors/
        QuickGitRefSelector.axaml
        QuickItemSelector.axaml
        QuickStringSelector.axaml
```

---

## Key technical challenge: the graph renderer

The graph renderer (`GraphRenderer.cs`) uses `System.Drawing.Graphics` to draw branch lanes as colored lines and curves. This must be ported to Avalonia's drawing API.

### Architecture of the renderer

1. `RevisionDataGridView` is a `DataGridView` (WinForms) with a custom cell painter
2. The `RevisionGraphColumnProvider` paints the graph column cell by cell
3. `GraphRenderer` renders each row's graph segment onto a `Graphics` context
4. `GraphCache` caches rendered rows as bitmaps for performance

### Avalonia approach

Replace the `DataGridView` with a virtualized `ItemsControl` (or Avalonia `DataGrid`) where each row is an `ItemTemplate`. The graph column cell is a custom `Control` that overrides `Render(DrawingContext ctx)`.

For the graph cache: use Avalonia `RenderTargetBitmap` to cache rendered segments.

### Drawing API mapping

```csharp
// WinForms
void DrawSegment(Graphics g, Color color, float x1, float y1, float x2, float y2)
{
    using var pen = new Pen(color, 2f);
    g.DrawLine(pen, x1, y1, x2, y2);
    g.FillEllipse(new SolidBrush(color), nodeRect);
}

// Avalonia
void DrawSegment(DrawingContext ctx, Avalonia.Media.Color color, double x1, double y1, double x2, double y2)
{
    var pen = new Pen(new SolidColorBrush(color), 2.0);
    ctx.DrawLine(pen, new Point(x1, y1), new Point(x2, y2));
    ctx.DrawEllipse(new SolidColorBrush(color), null, nodeCenter, radius, radius);
}
```

### Performance

The existing code uses `DataGridView` virtual mode (only renders visible rows). Replicate this with Avalonia's `VirtualizingStackPanel` in the `ItemsControl`. Only visible rows should be rendered.

---

## Ref labels

`RevisionGridRefRenderer` draws branch/tag labels inline in the commit message column. Port this to Avalonia by drawing `FormattedText` objects onto the message column's `DrawingContext`.

Branch labels: colored rounded rectangles with white text.  
Tag labels: different color, different shape.  
HEAD: bold, distinct color.

---

## Done Criteria

- [ ] Commit graph renders with colored branch lanes
- [ ] All standard columns show: graph, message (with ref labels), author, date, commit ID
- [ ] Clicking a commit selects it and the `SelectedRevisionChanged` event fires
- [ ] Right-clicking a commit shows the context menu with expected actions
- [ ] Scrolling through a large repo (1000+ commits) is smooth (virtualized)
- [ ] Branch/tag/HEAD ref labels display correctly in message column
- [ ] Revision filter dialog opens and filters the graph
- [ ] No WinForms references in any created file
- [ ] `docs/mac-port/TASK-STATUS.md` updated to `complete` for Task 3
