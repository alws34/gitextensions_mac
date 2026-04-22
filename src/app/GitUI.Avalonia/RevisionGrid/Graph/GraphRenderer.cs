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

            if (startIdx >= 0)
            {
                DrawCurve(ctx, pen, LaneCenterX(startIdx), 0, cx, midY);
            }

            if (endIdx >= 0)
            {
                DrawCurve(ctx, pen, cx, midY, LaneCenterX(endIdx), cellHeight);
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

    private static double LaneCenterX(int laneIndex) => (laneIndex + 0.5) * LaneWidth;
}
