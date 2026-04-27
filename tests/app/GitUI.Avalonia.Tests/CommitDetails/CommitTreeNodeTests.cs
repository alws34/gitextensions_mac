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
