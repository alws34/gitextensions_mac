using GitUI.Avalonia.CommitDetails;
using NUnit.Framework;

namespace GitUI.Avalonia.Tests.CommitDetails;

[TestFixture]
public class FileTreeNodeTests
{
    [Test]
    public void Build_FlatFile_ReturnsSingleRootChild()
    {
        var items = new[] { new FileStatusItem("README.md", false, false, false) };
        IReadOnlyList<FileTreeNode> nodes = FileTreeNode.BuildTree(items);
        Assert.That(nodes, Has.Count.EqualTo(1));
        Assert.That(nodes[0].Name, Is.EqualTo("README.md"));
        Assert.That(nodes[0].IsDirectory, Is.False);
    }

    [Test]
    public void Build_NestedFile_CreatesDirectoryNode()
    {
        var items = new[] { new FileStatusItem("src/Foo.cs", false, false, false) };
        IReadOnlyList<FileTreeNode> nodes = FileTreeNode.BuildTree(items);
        Assert.That(nodes, Has.Count.EqualTo(1));
        Assert.That(nodes[0].Name, Is.EqualTo("src"));
        Assert.That(nodes[0].IsDirectory, Is.True);
        Assert.That(nodes[0].Children, Has.Count.EqualTo(1));
        Assert.That(nodes[0].Children[0].Name, Is.EqualTo("Foo.cs"));
    }

    [Test]
    public void Build_SiblingFiles_GroupedUnderSameDirectory()
    {
        var items = new[]
        {
            new FileStatusItem("src/A.cs", false, false, false),
            new FileStatusItem("src/B.cs", false, false, false),
        };
        IReadOnlyList<FileTreeNode> nodes = FileTreeNode.BuildTree(items);
        Assert.That(nodes, Has.Count.EqualTo(1));
        Assert.That(nodes[0].Children, Has.Count.EqualTo(2));
    }

    [Test]
    public void Build_MixedDepths_RootFilesAndDirectories()
    {
        var items = new[]
        {
            new FileStatusItem("README.md", false, false, false),
            new FileStatusItem("src/Foo.cs", false, false, false),
        };
        IReadOnlyList<FileTreeNode> nodes = FileTreeNode.BuildTree(items);
        Assert.That(nodes, Has.Count.EqualTo(2));
    }
}
