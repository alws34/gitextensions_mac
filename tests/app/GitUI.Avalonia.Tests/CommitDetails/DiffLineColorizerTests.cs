using GitUI.Avalonia.CommitDetails;
using NUnit.Framework;

namespace GitUI.Avalonia.Tests.CommitDetails;

[TestFixture]
public class DiffLineColorizerTests
{
    [TestCase("+added line", DiffLineType.Added)]
    [TestCase("+++  new file", DiffLineType.FileHeader)]
    [TestCase("-removed line", DiffLineType.Removed)]
    [TestCase("--- a/foo.cs", DiffLineType.FileHeader)]
    [TestCase("@@ -1,3 +1,4 @@", DiffLineType.Section)]
    [TestCase(" context line", DiffLineType.Context)]
    [TestCase("diff --git a/foo b/foo", DiffLineType.Context)]
    [TestCase("", DiffLineType.Context)]
    public void GetLineType_ReturnsExpectedType(string line, DiffLineType expected)
    {
        Assert.That(DiffLineColorizer.GetLineType(line), Is.EqualTo(expected));
    }
}
