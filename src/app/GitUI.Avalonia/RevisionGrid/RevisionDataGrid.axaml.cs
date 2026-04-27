using Avalonia.Controls;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.RevisionGrid;

public partial class RevisionDataGrid : UserControl
{
    public event Action<GitRevision?>? SelectedRevisionChanged;

    public RevisionDataGrid() => InitializeComponent();

    public void LoadRevisions(IReadOnlyList<RevisionRow> rows)
    {
        CommitList.ItemsSource = rows;
    }

    public void ScrollToHash(string shortHash)
    {
        if (CommitList.ItemsSource is not IList<RevisionRow> rows)
        {
            return;
        }

        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].ShortHash.StartsWith(shortHash, StringComparison.OrdinalIgnoreCase))
            {
                CommitList.SelectedIndex = i;
                CommitList.ScrollIntoView(CommitList.SelectedItem!);
                return;
            }
        }
    }

    private void CommitList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectedRevisionChanged?.Invoke((CommitList.SelectedItem as RevisionRow)?.Revision);
    }
}
