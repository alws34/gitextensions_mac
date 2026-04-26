using GitUI.Avalonia.Converters;
using NUnit.Framework;

namespace GitUI.Avalonia.Tests.Converters;

[TestFixture]
public class RefTypeBrushConverterTests
{
    [Test]
    public void RefTypeKey_LocalBranch_ReturnsLocalKey()
    {
        Assert.That(RefTypeBrushConverter.GetResourceKey(isHead: true, isRemote: false, isTag: false),
            Is.EqualTo("RefLabelLocalBranch"));
    }

    [Test]
    public void RefTypeKey_Remote_ReturnsRemoteKey()
    {
        Assert.That(RefTypeBrushConverter.GetResourceKey(isHead: false, isRemote: true, isTag: false),
            Is.EqualTo("RefLabelRemoteBranch"));
    }

    [Test]
    public void RefTypeKey_Tag_ReturnsTagKey()
    {
        Assert.That(RefTypeBrushConverter.GetResourceKey(isHead: false, isRemote: false, isTag: true),
            Is.EqualTo("RefLabelTag"));
    }

    [Test]
    public void RefTypeKey_Unknown_ReturnsLocalKey()
    {
        Assert.That(RefTypeBrushConverter.GetResourceKey(isHead: false, isRemote: false, isTag: false),
            Is.EqualTo("RefLabelLocalBranch"));
    }
}
