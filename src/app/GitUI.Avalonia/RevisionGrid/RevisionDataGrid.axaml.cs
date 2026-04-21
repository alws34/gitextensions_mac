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

    private void CommitList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectedRevisionChanged?.Invoke((CommitList.SelectedItem as RevisionRow)?.Revision);
    }
}
