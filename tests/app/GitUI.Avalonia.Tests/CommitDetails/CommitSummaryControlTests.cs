using Avalonia.Controls;
using Avalonia.Media;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.CommitDetails;
using GitUIPluginInterfaces;
using NUnit.Framework;

namespace GitUI.Avalonia.Tests.CommitDetails;

[TestFixture]
public class CommitSummaryControlTests
{
    [SetUp]
    public void SetUp()
    {
        AvaloniaTestHost.EnsureStarted();
    }

    [Test]
    public void ShowRevision_LongSubject_WrapsInsideSummaryAndKeepsAuthorAndDateBelow()
    {
        var revision = new GitRevision(ObjectId.Parse("0123456789abcdef0123456789abcdef01234567"))
        {
            Subject = string.Join(' ', Enumerable.Repeat("long-subject-segment", 20)),
            Author = "A Very Long Author Name That Should Stay On Its Own Row",
            AuthorUnixTime = DateTimeOffset.Parse("2026-05-05T12:00:00+00:00").ToUnixTimeSeconds()
        };
        var control = new CommitSummaryControl();

        control.ShowRevision(revision);
        TextBlock message = Find<TextBlock>(control, "CommitMessage");
        TextBlock author = Find<TextBlock>(control, "AuthorName");
        TextBlock date = Find<TextBlock>(control, "AuthorDate");

        Assert.Multiple(() =>
        {
            Assert.That(message.TextWrapping, Is.EqualTo(TextWrapping.Wrap));
            Assert.That(message.MaxLines, Is.EqualTo(2));
            Assert.That(message.TextTrimming, Is.EqualTo(TextTrimming.CharacterEllipsis));
            Assert.That(Grid.GetRow(message), Is.EqualTo(1));
            Assert.That(Grid.GetRow(author), Is.EqualTo(2));
            Assert.That(Grid.GetRow(date), Is.EqualTo(3));
        });
    }

    private static T Find<T>(Control control, string name)
        where T : Control
        => control.FindControl<T>(name)
           ?? throw new InvalidOperationException($"Control {name} was not loaded.");
}
