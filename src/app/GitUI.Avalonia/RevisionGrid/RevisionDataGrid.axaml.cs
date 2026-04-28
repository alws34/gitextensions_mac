using Avalonia.Controls;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.RevisionGrid;

public partial class RevisionDataGrid : UserControl
{
    public event Action<GitRevision?>? SelectedRevisionChanged;

    // Events — bubble up to RevisionGridControl / MainWindow
    public event Action<string>? CheckoutHashRequested;
    public event Action<string>? CherryPickHashRequested;
    public event Action<string>? RevertHashRequested;
    public event Action<string>? CreateBranchAtHashRequested;
    public event Action<string>? CreateTagAtHashRequested;
    public event Action<string>? ResetHardToHashRequested;
    public event Action<string>? InteractiveRebaseRequested;

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

    private RevisionRow? GetSelectedRow() => CommitList.SelectedItem as RevisionRow;

    private void CommitContextMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (GetSelectedRow() is null)
        {
            e.Cancel = true;
        }
    }

    private void CtxCheckout_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        string? hash = GetSelectedRow()?.Revision.Guid;
        if (hash is not null)
        {
            CheckoutHashRequested?.Invoke(hash);
        }
    }

    private void CtxCherryPick_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        string? hash = GetSelectedRow()?.Revision.Guid;
        if (hash is not null)
        {
            CherryPickHashRequested?.Invoke(hash);
        }
    }

    private void CtxRevert_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        string? hash = GetSelectedRow()?.Revision.Guid;
        if (hash is not null)
        {
            RevertHashRequested?.Invoke(hash);
        }
    }

    private void CtxCreateBranch_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        string? hash = GetSelectedRow()?.Revision.Guid;
        if (hash is not null)
        {
            CreateBranchAtHashRequested?.Invoke(hash);
        }
    }

    private void CtxCreateTag_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        string? hash = GetSelectedRow()?.Revision.Guid;
        if (hash is not null)
        {
            CreateTagAtHashRequested?.Invoke(hash);
        }
    }

    private void CtxResetHard_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        string? hash = GetSelectedRow()?.Revision.Guid;
        if (hash is not null)
        {
            ResetHardToHashRequested?.Invoke(hash);
        }
    }

    private void CtxCopyHash_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        string? hash = GetSelectedRow()?.ShortHash;
        if (hash is not null)
        {
            _ = global::Avalonia.Controls.TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(hash);
        }
    }

    private void CtxInteractiveRebase_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        string? hash = GetSelectedRow()?.Revision.Guid;
        if (hash is not null)
        {
            InteractiveRebaseRequested?.Invoke(hash);
        }
    }
}
