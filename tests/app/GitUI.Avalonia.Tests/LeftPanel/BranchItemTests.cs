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

    [Test]
    public void BuildBranchTree_GroupsNestedBranchesIntoFolders()
    {
        var branches = new[]
        {
            new BranchItem("feature/search/menu-tests", IsCurrent: false),
            new BranchItem("feature/search/tree-tests", IsCurrent: true),
            new BranchItem("main", IsCurrent: false),
        };

        IReadOnlyList<BranchItem> tree = RepoBrowserPanel.BuildBranchTree(
            branches,
            filter: string.Empty,
            RefSortMode.Ascending,
            rootFoldersExpanded: true);

        BranchItem feature = tree.Single(i => i.DisplayName == "feature");
        BranchItem search = feature.Children.Single(i => i.DisplayName == "search");

        Assert.Multiple(() =>
        {
            Assert.That(feature.Kind, Is.EqualTo(BranchItemKind.Folder));
            Assert.That(feature.IsExpanded, Is.True);
            Assert.That(search.Children.Select(i => i.Name), Is.EqualTo(new[] { "feature/search/menu-tests", "feature/search/tree-tests" }));
            Assert.That(search.Children.Single(i => i.DisplayName == "tree-tests").IsCurrent, Is.True);
        });
    }

    [Test]
    public void BuildRemoteTree_UsesRemoteNameAsRoot()
    {
        IReadOnlyList<BranchItem> tree = RepoBrowserPanel.BuildRemoteTree(
            new[] { "origin/main", "upstream/releases/1.0" },
            filter: string.Empty,
            RefSortMode.Ascending,
            rootFoldersExpanded: false);

        Assert.Multiple(() =>
        {
            Assert.That(tree.Select(i => i.DisplayName), Is.EqualTo(new[] { "origin", "upstream" }));
            Assert.That(tree.Single(i => i.DisplayName == "origin").Children.Single().Name, Is.EqualTo("origin/main"));
            Assert.That(tree.Single(i => i.DisplayName == "upstream").IsExpanded, Is.False);
        });
    }

    [Test]
    public void BuildBranchTree_FilterKeepsMatchingDescendantAndAncestors()
    {
        var branches = new[]
        {
            new BranchItem("feature/search/menu-tests", IsCurrent: false),
            new BranchItem("feature/api/work", IsCurrent: false),
            new BranchItem("main", IsCurrent: false),
        };

        IReadOnlyList<BranchItem> tree = RepoBrowserPanel.BuildBranchTree(
            branches,
            filter: "menu",
            RefSortMode.Ascending,
            rootFoldersExpanded: true);

        Assert.That(tree, Has.Count.EqualTo(1));
        BranchItem feature = tree[0];
        Assert.That(feature.Children, Has.Count.EqualTo(1));
        BranchItem search = feature.Children[0];
        Assert.That(search.Children, Has.Count.EqualTo(1));
        BranchItem leaf = search.Children[0];

        Assert.That(leaf.Name, Is.EqualTo("feature/search/menu-tests"));
    }

    [Test]
    public void BuildBranchTree_DescendingSortKeepsFoldersFirst()
    {
        var branches = new[]
        {
            new BranchItem("zulu", IsCurrent: false),
            new BranchItem("feature/beta", IsCurrent: false),
            new BranchItem("alpha", IsCurrent: false),
        };

        IReadOnlyList<BranchItem> tree = RepoBrowserPanel.BuildBranchTree(
            branches,
            filter: string.Empty,
            RefSortMode.Descending,
            rootFoldersExpanded: true);

        Assert.That(tree.Select(i => i.DisplayName), Is.EqualTo(new[] { "feature", "zulu", "alpha" }));
    }
}
