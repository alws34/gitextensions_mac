using GitUI.Avalonia.Base;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.CommitDetails;

public partial class CommitSummaryControl : GitModuleControl
{
    public CommitSummaryControl() => InitializeComponent();

    public void ShowRevision(GitRevision? revision)
    {
        CommitHash.Text = revision?.Guid ?? string.Empty;
        CommitMessage.Text = revision?.Subject ?? string.Empty;
        AuthorName.Text = revision?.Author ?? string.Empty;
        AuthorDate.Text = revision?.AuthorDate.ToString("yyyy-MM-dd HH:mm") ?? string.Empty;

        if (revision?.Refs is { Count: > 0 } refs)
        {
            RefLabels.ItemsSource = refs;
            RefLabels.IsVisible = true;
        }
        else
        {
            RefLabels.IsVisible = false;
        }
    }
}
