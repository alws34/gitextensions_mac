using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GitExtensions.Extensibility.Git;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.RevisionGrid;

public partial class RevisionDataGrid : UserControl
{
    public event Action<GitRevision?>? SelectedRevisionChanged;
    public event Action<RevisionRow?>? SelectedRowChanged;

    // Events — bubble up to RevisionGridControl / MainWindow
    public event Action<string>? CheckoutHashRequested;
    public event Action<string>? CheckoutBranchRequested;
    public event Action<string>? CheckoutRemoteBranchRequested;
    public event Action<string>? MergeRefRequested;
    public event Action<string>? RebaseRefRequested;
    public event Action<string>? RenameBranchRequested;
    public event Action<string>? DeleteBranchRequested;
    public event Action<string>? DeleteRemoteBranchRequested;
    public event Action<string>? DeleteTagRequested;
    public event Action<string>? PushBranchRequested;
    public event Action<string>? CherryPickHashRequested;
    public event Action<string>? RevertHashRequested;
    public event Action<string>? CreateBranchAtHashRequested;
    public event Action<string>? CreateBranchAtRefRequested;
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
    public string CurrentBranchName { get; set; } = string.Empty;
    public string CurrentHeadHash { get; set; } = string.Empty;

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

        if (hasCommit)
        {
            List<IGitRef> refs = row.Refs
                .Where(gitRef => !gitRef.IsDereference)
                .ToList();
            List<IGitRef> localBranches = refs
                .Where(gitRef => gitRef.IsHead)
                .OrderBy(gitRef => gitRef.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            List<IGitRef> otherLocalBranches = localBranches
                .Where(gitRef => !string.Equals(gitRef.LocalName, CurrentBranchName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            List<IGitRef> remoteBranches = refs
                .Where(gitRef => gitRef.IsRemote)
                .OrderBy(gitRef => gitRef.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            List<IGitRef> tags = refs
                .Where(gitRef => gitRef.IsTag)
                .OrderBy(gitRef => gitRef.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            bool selectedRowIsCurrentHead = !string.IsNullOrEmpty(CurrentHeadHash)
                && string.Equals(row.Revision?.Guid, CurrentHeadHash, StringComparison.OrdinalIgnoreCase);

            BuildRefMenu(
                CheckoutBranchMenuItem,
                otherLocalBranches,
                "Checkout branch",
                gitRef => gitRef.Name,
                CtxCheckoutBranch_Click);

            BuildRefMenu(
                CheckoutRemoteBranchMenuItem,
                remoteBranches,
                "Checkout remote branch as local",
                gitRef => gitRef.Name,
                CtxCheckoutRemoteBranch_Click);

            BuildRefMenu(
                MergeRefMenuItem,
                selectedRowIsCurrentHead ? [] : [.. otherLocalBranches, .. remoteBranches, .. tags],
                "Merge into current branch",
                gitRef => gitRef.Name,
                CtxMergeRef_Click);

            BuildRefMenu(
                RebaseRefMenuItem,
                selectedRowIsCurrentHead ? [] : [.. otherLocalBranches, .. remoteBranches],
                "Rebase current branch onto this",
                gitRef => gitRef.Name,
                CtxRebaseRef_Click);

            BuildRefMenu(
                RenameBranchMenuItem,
                localBranches,
                "Rename branch",
                gitRef => gitRef.Name,
                CtxRenameBranch_Click);

            BuildRefMenu(
                DeleteBranchMenuItem,
                otherLocalBranches,
                "Delete branch",
                gitRef => gitRef.Name,
                CtxDeleteBranch_Click);

            BuildRefMenu(
                DeleteRemoteBranchMenuItem,
                remoteBranches,
                "Delete remote branch",
                gitRef => gitRef.Name,
                CtxDeleteRemoteBranch_Click);

            BuildRefMenu(
                DeleteTagMenuItem,
                tags,
                "Delete tag",
                gitRef => gitRef.Name,
                CtxDeleteTag_Click);

            BuildRefMenu(
                PushBranchMenuItem,
                [.. localBranches, .. remoteBranches],
                "Push branch",
                gitRef => gitRef.Name,
                CtxPushBranch_Click);

            BuildRefMenu(
                CreateBranchAtRefMenuItem,
                [.. localBranches, .. remoteBranches, .. tags],
                "Create branch at ref",
                gitRef => gitRef.Name,
                CtxCreateBranchAtRef_Click);

            BuildRefMenu(
                CopyRefNameMenuItem,
                refs.OrderBy(gitRef => gitRef.Name, StringComparer.OrdinalIgnoreCase).ToList(),
                "Copy ref name",
                gitRef => gitRef.Name,
                CtxCopyRefName_Click);
        }
        else
        {
            HideRefMenuItems();
        }

        bool hasSubject = !string.IsNullOrEmpty(row.Subject);
        CopySubjectMenuItem.IsVisible = hasSubject;
        CopySubjectMenuItem.IsEnabled = hasSubject;

        UpdateSeparators(CommitContextMenu);
    }

    private static void BuildRefMenu(
        MenuItem menuItem,
        IReadOnlyList<IGitRef> refs,
        string header,
        Func<IGitRef, string> refName,
        EventHandler<RoutedEventArgs> clickHandler)
    {
        menuItem.Items.Clear();
        menuItem.Tag = null;
        menuItem.IsVisible = refs.Count > 0;
        menuItem.IsEnabled = refs.Count > 0;
        if (refs.Count == 0)
        {
            menuItem.Header = header;
            return;
        }

        if (refs.Count == 1)
        {
            string name = refName(refs[0]);
            menuItem.Header = $"{header} '{name}'";
            menuItem.Tag = name;
            return;
        }

        menuItem.Header = header;
        foreach (IGitRef gitRef in refs)
        {
            string name = refName(gitRef);
            var child = new MenuItem
            {
                Header = name,
                Tag = name,
            };
            child.Click += clickHandler;
            menuItem.Items.Add(child);
        }
    }

    private void CtxCheckout_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        string? hash = GetSelectedCommitHash();
        if (hash is not null)
        {
            CheckoutHashRequested?.Invoke(hash);
        }
    }

    private void CtxCheckoutBranch_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetMenuStringTag(sender) is { } branch)
        {
            CheckoutBranchRequested?.Invoke(branch);
        }
    }

    private void CtxCheckoutRemoteBranch_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetMenuStringTag(sender) is { } remoteBranch)
        {
            CheckoutRemoteBranchRequested?.Invoke(remoteBranch);
        }
    }

    private void CtxMergeRef_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetMenuStringTag(sender) is { } refName)
        {
            MergeRefRequested?.Invoke(refName);
        }
    }

    private void CtxRebaseRef_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetMenuStringTag(sender) is { } refName)
        {
            RebaseRefRequested?.Invoke(refName);
        }
    }

    private void CtxRenameBranch_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetMenuStringTag(sender) is { } branch)
        {
            RenameBranchRequested?.Invoke(branch);
        }
    }

    private void CtxDeleteBranch_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetMenuStringTag(sender) is { } branch)
        {
            DeleteBranchRequested?.Invoke(branch);
        }
    }

    private void CtxDeleteRemoteBranch_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetMenuStringTag(sender) is { } branch)
        {
            DeleteRemoteBranchRequested?.Invoke(branch);
        }
    }

    private void CtxDeleteTag_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetMenuStringTag(sender) is { } tag)
        {
            DeleteTagRequested?.Invoke(tag);
        }
    }

    private void CtxPushBranch_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetMenuStringTag(sender) is { } branch)
        {
            PushBranchRequested?.Invoke(branch);
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

    private void CtxCreateBranchAtRef_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetMenuStringTag(sender) is { } refName)
        {
            CreateBranchAtRefRequested?.Invoke(refName);
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

    private void CtxCopyRefName_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetMenuStringTag(sender) is { } refName)
        {
            CopyToClipboard(refName);
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

    private static string? GetMenuStringTag(object? sender) =>
        sender is MenuItem { Tag: string value } && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;

    private void HideRefMenuItems()
    {
        foreach (MenuItem menuItem in GetRefMenuItems())
        {
            menuItem.Items.Clear();
            menuItem.Tag = null;
            menuItem.IsVisible = false;
            menuItem.IsEnabled = false;
        }
    }

    private IEnumerable<MenuItem> GetRefMenuItems()
    {
        yield return CheckoutBranchMenuItem;
        yield return CheckoutRemoteBranchMenuItem;
        yield return MergeRefMenuItem;
        yield return RebaseRefMenuItem;
        yield return RenameBranchMenuItem;
        yield return DeleteBranchMenuItem;
        yield return DeleteRemoteBranchMenuItem;
        yield return DeleteTagMenuItem;
        yield return PushBranchMenuItem;
        yield return CreateBranchAtRefMenuItem;
        yield return CopyRefNameMenuItem;
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
