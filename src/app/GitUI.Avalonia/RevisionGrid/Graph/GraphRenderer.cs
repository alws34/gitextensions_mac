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
