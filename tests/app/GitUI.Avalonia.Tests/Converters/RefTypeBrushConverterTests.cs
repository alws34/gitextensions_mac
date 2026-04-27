using GitUI.Avalonia.Converters;
using NUnit.Framework;

namespace GitUI.Avalonia.Tests.Converters;

[TestFixture]
public class RefTypeBrushConverterTests
{
    [Test]
    public void RefTypeKey_LocalBranch_ReturnsLocalKey()
    {
        Assert.That(RefTypeBrushConverter.GetResourceKey(isRemote: false, isTag: false),
            Is.EqualTo("RefBadgeLocalBranchBackground"));
    }

    [Test]
    public void RefTypeKey_Remote_ReturnsRemoteKey()
    {
        Assert.That(RefTypeBrushConverter.GetResourceKey(isRemote: true, isTag: false),
            Is.EqualTo("RefBadgeRemoteBranchBackground"));
    }

    [Test]
    public void RefTypeKey_Tag_ReturnsTagKey()
    {
        Assert.That(RefTypeBrushConverter.GetResourceKey(isRemote: false, isTag: true),
            Is.EqualTo("RefBadgeTagBackground"));
    }

    [Test]
    public void RefTypeKey_Unknown_ReturnsLocalKey()
    {
        Assert.That(RefTypeBrushConverter.GetResourceKey(isRemote: false, isTag: false),
            Is.EqualTo("RefBadgeLocalBranchBackground"));
    }
}
