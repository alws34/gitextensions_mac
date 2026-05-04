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
        AuthorDate.Text = FormatDate(revision?.AuthorDate);

        BodyScroll.IsVisible = false;
        CommitBody.Text = string.Empty;

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

    public void ShowBody(string? body)
    {
        bool hasBody = !string.IsNullOrWhiteSpace(body);
        CommitBody.Text = hasBody ? body!.Trim() : string.Empty;
        BodyScroll.IsVisible = hasBody;
    }

    private static string FormatDate(DateTimeOffset? date)
    {
        if (date is null)
        {
            return string.Empty;
        }

        TimeSpan ago = DateTimeOffset.Now - date.Value;
        string relative = ago.TotalDays < 1 ? $"{(int)ago.TotalHours}h ago"
                        : ago.TotalDays < 7 ? $"{(int)ago.TotalDays}d ago"
                        : ago.TotalDays < 30 ? $"{(int)(ago.TotalDays / 7)}w ago"
                        : date.Value.ToString("yyyy-MM-dd");
        return $"{date.Value:yyyy-MM-dd HH:mm}  ({relative})";
    }
}
