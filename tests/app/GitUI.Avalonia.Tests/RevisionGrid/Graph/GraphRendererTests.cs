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
        double rowNBottom = GraphRenderer.BoundaryX(1, 3);
        double rowN1Top = GraphRenderer.BoundaryX(3, 1);
        Assert.That(rowNBottom, Is.EqualTo(rowN1Top));
    }
}
