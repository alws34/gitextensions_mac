using GitUI.Avalonia.CommitDetails;
using NUnit.Framework;

namespace GitUI.Avalonia.Tests.CommitDetails;

[TestFixture]
public class CommitDiffControlTests
{
    [Test]
    public void BuildDiffTreeArguments_UsesRecursiveRootPatch()
    {
        string args = CommitDiffControl.BuildDiffTreeArguments(
            "0123456789abcdef0123456789abcdef01234567",
            filePath: null);

        Assert.That(
            args,
            Is.EqualTo("diff-tree --no-commit-id -p --root -r 0123456789abcdef0123456789abcdef01234567"));
    }

    [Test]
    public void BuildDiffTreeArguments_QuotesSelectedPath()
    {
        string args = CommitDiffControl.BuildDiffTreeArguments(
            "0123456789abcdef0123456789abcdef01234567",
            "src/app/file with spaces.cs");

        Assert.That(
            args,
            Is.EqualTo("diff-tree --no-commit-id -p --root -r 0123456789abcdef0123456789abcdef01234567 -- \"src/app/file with spaces.cs\""));
    }
}
