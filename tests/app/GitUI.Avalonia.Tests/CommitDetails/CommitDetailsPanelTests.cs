using Avalonia.Controls;
using GitUI.Avalonia.CommitDetails;
using NUnit.Framework;

namespace GitUI.Avalonia.Tests.CommitDetails;

[TestFixture]
public class CommitDetailsPanelTests
{
    [SetUp]
    public void SetUp()
    {
        AvaloniaTestHost.EnsureStarted();
    }

    [Test]
    public void PrimaryDiffTab_ShowsFileListAndDiffTogether()
    {
        var panel = new CommitDetailsPanel();
        TabControl tabs = panel.FindControl<TabControl>("DetailsTabs")
            ?? throw new InvalidOperationException("DetailsTabs was not loaded.");

        var tabItems = tabs.Items.OfType<TabItem>().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(tabItems.Select(item => item.Header), Is.EqualTo(new[] { "Diff", "Tree" }));
            Assert.That(panel.FindControl<FileStatusList>("FileList"), Is.Not.Null);
            Assert.That(panel.FindControl<CommitDiffControl>("DiffView"), Is.Not.Null);
        });
    }
}
