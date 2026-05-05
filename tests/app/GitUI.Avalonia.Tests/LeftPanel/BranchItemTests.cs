using GitUI.Avalonia.LeftPanel;
using NUnit.Framework;

namespace GitUI.Avalonia.Tests.LeftPanel;

[TestFixture]
public class BranchItemTests
{
    [Test]
    public void DisplayName_NestedBranch_UsesLeafName()
    {
        var branch = new BranchItem("feature/search/menu-tests", IsCurrent: false);

        Assert.That(branch.DisplayName, Is.EqualTo("menu-tests"));
    }

    [Test]
    public void Depth_NestedBranch_CountsPathSeparators()
    {
        var branch = new BranchItem("feature/search/menu-tests", IsCurrent: false);

        Assert.That(branch.Depth, Is.EqualTo(2));
    }

    [Test]
    public void CurrentBranch_UsesCurrentIconAndColor()
    {
        var branch = new BranchItem("main", IsCurrent: true);

        Assert.Multiple(() =>
        {
            Assert.That(branch.Icon, Is.EqualTo("●"));
            Assert.That(branch.IconColor, Is.EqualTo("#FF2E7D32"));
            Assert.That(branch.HasAheadBehind, Is.False);
            Assert.That(branch.AheadBehind, Is.Empty);
        });
    }
}
