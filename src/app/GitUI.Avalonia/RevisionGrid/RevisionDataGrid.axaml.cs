using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.RevisionGrid;

public partial class RevisionDataGrid : UserControl
{
    public event Action<GitRevision?>? SelectedRevisionChanged;
    public event Action<RevisionRow?>? SelectedRowChanged;

    // Events — bubble up to RevisionGridControl / MainWindow
    public event Action<string>? CheckoutHashRequested;
    public event Action<string>? CherryPickHashRequested;
    public event Action<string>? RevertHashRequested;
    public event Action<string>? CreateBranchAtHashRequested;
    public event Action<string>? CreateTagAtHashRequested;
    public event Action<string>? ResetHardToHashRequested;
    public event Action<string>? InteractiveRebaseRequested;

    // Column width styled properties — bound from the row DataTemplate via $parent[RevisionDataGrid]
    public static readonly StyledProperty<GridLength> GraphColWidthProperty =
        AvaloniaProperty.Register<RevisionDataGrid, GridLength>(nameof(GraphColWidth), new GridLength(80));
    public static readonly StyledProperty<GridLength> AuthorColWidthProperty =
        AvaloniaProperty.Register<RevisionDataGrid, GridLength>(nameof(AuthorColWidth), new GridLength(140));
    public static readonly StyledProperty<GridLength> DateColWidthProperty =
        AvaloniaProperty.Register<RevisionDataGrid, GridLength>(nameof(DateColWidth), new GridLength(100));
    public static readonly StyledProperty<GridLength> HashColWidthProperty =
        AvaloniaProperty.Register<RevisionDataGrid, GridLength>(nameof(HashColWidth), new GridLength(72));

    public GridLength GraphColWidth { get => GetValue(GraphColWidthProperty); set => SetValue(GraphColWidthProperty, value); }
    public GridLength AuthorColWidth { get => GetValue(AuthorColWidthProperty); set => SetValue(AuthorColWidthProperty, value); }
    public GridLength DateColWidth { get => GetValue(DateColWidthProperty); set => SetValue(DateColWidthProperty, value); }
    public GridLength HashColWidth { get => GetValue(HashColWidthProperty); set => SetValue(HashColWidthProperty, value); }

    public RevisionDataGrid() => InitializeComponent();

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // When header GridSplitters are dragged, propagate new widths to row bindings
        HeaderGrid.ColumnDefinitions[0].PropertyChanged += (_, _) =>
            GraphColWidth = new GridLength(HeaderGrid.ColumnDefinitions[0].ActualWidth);
        HeaderGrid.ColumnDefinitions[4].PropertyChanged += (_, _) =>
            AuthorColWidth = new GridLength(HeaderGrid.ColumnDefinitions[4].ActualWidth);
        HeaderGrid.ColumnDefinitions[6].PropertyChanged += (_, _) =>
            DateColWidth = new GridLength(HeaderGrid.ColumnDefinitions[6].ActualWidth);
        HeaderGrid.ColumnDefinitions[8].PropertyChanged += (_, _) =>
            HashColWidth = new GridLength(HeaderGrid.ColumnDefinitions[8].ActualWidth);
    }

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

    public void SelectRelative(int offset)
    {
        int nextIndex = CommitList.SelectedIndex + offset;
        if (nextIndex < 0 || nextIndex >= CommitList.ItemCount)
        {
            return;
        }

        CommitList.SelectedIndex = nextIndex;
        if (CommitList.SelectedItem is { } selectedItem)
        {
            CommitList.ScrollIntoView(selectedItem);
        }
    }

    private void CommitList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var row = CommitList.SelectedItem as RevisionRow;
        SelectedRowChanged?.Invoke(row);
        SelectedRevisionChanged?.Invoke(row?.Revision);
    }

    private RevisionRow? GetSelectedRow() => CommitList.SelectedItem as RevisionRow;

    private string? GetSelectedCommitHash() => GetSelectedRow()?.Revision?.Guid;

    private void CommitContextMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        RevisionRow? row = GetSelectedRow();
        if (row is null)
        {
            e.Cancel = true;
            return;
        }

        bool hasCommit = row.Revision is not null;
        foreach (MenuItem menuItem in GetCommitOnlyMenuItems())
        {
            menuItem.IsVisible = hasCommit;
            menuItem.IsEnabled = hasCommit;
        }

        bool hasSubject = !string.IsNullOrEmpty(row.Subject);
        CopySubjectMenuItem.IsVisible = hasSubject;
        CopySubjectMenuItem.IsEnabled = hasSubject;

        UpdateSeparators(CommitContextMenu);
    }

    private void CtxCheckout_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        string? hash = GetSelectedCommitHash();
        if (hash is not null)
        {
            CheckoutHashRequested?.Invoke(hash);
        }
    }

    private void CtxCherryPick_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        string? hash = GetSelectedCommitHash();
        if (hash is not null)
        {
            CherryPickHashRequested?.Invoke(hash);
        }
    }

    private void CtxRevert_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        string? hash = GetSelectedCommitHash();
        if (hash is not null)
        {
            RevertHashRequested?.Invoke(hash);
        }
    }

    private void CtxCreateBranch_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        string? hash = GetSelectedCommitHash();
        if (hash is not null)
        {
            CreateBranchAtHashRequested?.Invoke(hash);
        }
    }

    private void CtxCreateTag_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        string? hash = GetSelectedCommitHash();
        if (hash is not null)
        {
            CreateTagAtHashRequested?.Invoke(hash);
        }
    }

    private void CtxResetHard_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        string? hash = GetSelectedCommitHash();
        if (hash is not null)
        {
            ResetHardToHashRequested?.Invoke(hash);
        }
    }

    private void CtxCopyShortHash_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetSelectedRow() is { Revision: not null } row)
        {
            CopyToClipboard(row.ShortHash);
        }
    }

    private void CtxCopyFullHash_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        CopyToClipboard(GetSelectedCommitHash());
    }

    private void CtxCopySubject_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        CopyToClipboard(GetSelectedRow()?.Subject);
    }

    private void CtxCopyCommitSummary_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetSelectedRow() is { Revision: not null } row)
        {
            CopyToClipboard(string.IsNullOrWhiteSpace(row.Subject)
                ? row.ShortHash
                : $"{row.ShortHash} {row.Subject}");
        }
    }

    private void CtxInteractiveRebase_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        string? hash = GetSelectedCommitHash();
        if (hash is not null)
        {
            InteractiveRebaseRequested?.Invoke(hash);
        }
    }

    private IEnumerable<MenuItem> GetCommitOnlyMenuItems()
    {
        yield return CheckoutCommitMenuItem;
        yield return CherryPickCommitMenuItem;
        yield return RevertCommitMenuItem;
        yield return CreateBranchMenuItem;
        yield return CreateTagMenuItem;
        yield return ResetHardMenuItem;
        yield return InteractiveRebaseMenuItem;
        yield return CopyShortHashMenuItem;
        yield return CopyFullHashMenuItem;
        yield return CopyCommitSummaryMenuItem;
    }

    private void CopyToClipboard(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        _ = global::Avalonia.Controls.TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(text);
    }

    private static void UpdateSeparators(ContextMenu menu)
    {
        bool hasVisibleItemBeforeSeparator = false;
        Separator? pendingSeparator = null;

        foreach (object? item in menu.Items)
        {
            if (item is Separator separator)
            {
                separator.IsVisible = false;
                pendingSeparator = separator;
                continue;
            }

            if (item is MenuItem { IsVisible: true })
            {
                if (hasVisibleItemBeforeSeparator && pendingSeparator is not null)
                {
                    pendingSeparator.IsVisible = true;
                }

                hasVisibleItemBeforeSeparator = true;
                pendingSeparator = null;
            }
        }
    }
}
