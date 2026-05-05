using GitUI.Avalonia.CommitDetails;
using NUnit.Framework;

namespace GitUI.Avalonia.Tests.CommitDetails;

[TestFixture]
public class FileStatusItemTests
{
    [TestCase(true, false, false, "A")]
    [TestCase(false, true, false, "D")]
    [TestCase(false, false, true, "R")]
    [TestCase(false, false, false, "M")]
    public void StatusIcon_MapsStatusFlags(
        bool isAdded,
        bool isDeleted,
        bool isRenamed,
        string expectedIcon)
    {
        var item = new FileStatusItem("src/Foo.cs", isAdded, isDeleted, isRenamed);

        Assert.That(item.StatusIcon, Is.EqualTo(expectedIcon));
    }
}
