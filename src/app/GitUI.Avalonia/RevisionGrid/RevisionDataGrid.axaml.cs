using Avalonia.Controls;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.RevisionGrid;

public partial class RevisionDataGrid : UserControl
{
    public event Action<GitRevision?>? SelectedRevisionChanged;

    public RevisionDataGrid() => InitializeComponent();

    public void LoadRevisions(IReadOnlyList<GitRevision> revisions)
    {
        CommitList.ItemsSource = revisions;
    }

    private void CommitList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectedRevisionChanged?.Invoke(CommitList.SelectedItem as GitRevision);
    }
}
