using Avalonia;
using Avalonia.Media;

namespace GitUI.Avalonia.RevisionGrid.Graph;

internal static class GraphRenderer
{
    private const int NodeRadius = 4;
    private const double LaneWidth = 16.0;

    public static void RenderGraphCell(
        DrawingContext ctx,
        IRevisionGraphRow row,
        double cellWidth,
        double cellHeight,
        IReadOnlyList<Color> laneColors)
    {
        double midY = cellHeight / 2.0;
        int laneCount = row.GetLaneCount();

        for (int i = 0; i < laneCount; i++)
        {
            foreach (var segment in row.GetSegmentsForIndex(i))
            {
                int colorIdx = RevisionGraphLaneColor.GetColorForLane(segment.GetHashCode());
                var color = colorIdx < laneColors.Count ? laneColors[colorIdx] : Colors.Gray;
                var pen = new Pen(new SolidColorBrush(color), 2.0);
                double x = LaneCenterX(i);
                ctx.DrawLine(pen, new Point(x, 0), new Point(x, cellHeight));
            }
        }

        int nodeLane = row.GetCurrentRevisionLane();
        if (nodeLane >= 0 && nodeLane < laneColors.Count)
        {
            var nodeColor = laneColors[nodeLane];
            double nodeX = LaneCenterX(nodeLane);
            ctx.DrawEllipse(
                new SolidColorBrush(nodeColor),
                new Pen(Brushes.White, 1.0),
                new Point(nodeX, midY),
                NodeRadius,
                NodeRadius);
        }
    }

    private static double LaneCenterX(int laneIndex) => (laneIndex + 0.5) * LaneWidth;
}
