using System.ComponentModel;
using System.Reflection;
using Avalonia.Controls;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.RevisionGrid;
using GitUIPluginInterfaces;
using NUnit.Framework;

namespace GitUI.Avalonia.Tests.RevisionGrid;

[TestFixture]
public class RevisionDataGridContextMenuTests
{
    [SetUp]
    public void SetUp()
    {
        AvaloniaTestHost.EnsureStarted();
    }

    [Test]
    public void ContextMenuOpening_NoSelection_CancelsMenu()
    {
        var grid = new RevisionDataGrid();
        var args = new CancelEventArgs();

        InvokePrivate(grid, "CommitContextMenu_Opening", GetContextMenu(grid), args);

        Assert.That(args.Cancel, Is.True);
    }

    [Test]
    public void ContextMenuOpening_ArtificialRow_AllowsMenuButHashActionsDoNotRaiseEvents()
    {
        var grid = new RevisionDataGrid();
        var row = new RevisionRow(GitRevision.WorkTreeGuid, "Working tree changes", "WorkTree");
        grid.LoadRevisions([row]);
        GetCommitList(grid).SelectedIndex = 0;

        var args = new CancelEventArgs();
        string? checkoutHash = null;
        string? branchHash = null;
        grid.CheckoutHashRequested += hash => checkoutHash = hash;
        grid.CreateBranchAtHashRequested += hash => branchHash = hash;

        InvokePrivate(grid, "CommitContextMenu_Opening", GetContextMenu(grid), args);
        InvokePrivate(grid, "CtxCheckout_Click", null, null);
        InvokePrivate(grid, "CtxCreateBranch_Click", null, null);

        Assert.Multiple(() =>
        {
            Assert.That(args.Cancel, Is.False);
            Assert.That(checkoutHash, Is.Null);
            Assert.That(branchHash, Is.Null);
        });
    }

    [Test]
    public void ContextMenuAction_NormalRevision_RaisesHashEvent()
    {
        var objectId = ObjectId.Parse("0123456789abcdef0123456789abcdef01234567");
        var revision = new GitRevision(objectId);
        var grid = new RevisionDataGrid();
        grid.LoadRevisions([new RevisionRow(revision, graphRow: null, prevRow: null, nextRow: null)]);
        GetCommitList(grid).SelectedIndex = 0;

        string? checkoutHash = null;
        grid.CheckoutHashRequested += hash => checkoutHash = hash;

        InvokePrivate(grid, "CtxCheckout_Click", null, null);

        Assert.That(checkoutHash, Is.EqualTo(revision.Guid));
    }

    [Test]
    public void ContextMenuOpening_RowWithAlternateLocalBranch_ShowsCheckoutBranchAction()
    {
        var objectId = ObjectId.Parse("0123456789abcdef0123456789abcdef01234567");
        var revision = new GitRevision(objectId)
        {
            Refs =
            [
                new GitRef(null!, objectId, "refs/heads/main"),
                new GitRef(null!, objectId, "refs/heads/feature/menu")
            ]
        };
        var grid = new RevisionDataGrid
        {
            CurrentBranchName = "main"
        };
        grid.LoadRevisions([new RevisionRow(revision, graphRow: null, prevRow: null, nextRow: null)]);
        GetCommitList(grid).SelectedIndex = 0;

        var args = new CancelEventArgs();
        InvokePrivate(grid, "CommitContextMenu_Opening", GetContextMenu(grid), args);

        string? checkoutBranch = null;
        grid.CheckoutBranchRequested += branch => checkoutBranch = branch;
        MenuItem checkoutBranchMenuItem = GetMenuItem(grid, "CheckoutBranchMenuItem");
        InvokePrivate(grid, "CtxCheckoutBranch_Click", checkoutBranchMenuItem, null);

        Assert.Multiple(() =>
        {
            Assert.That(args.Cancel, Is.False);
            Assert.That(checkoutBranchMenuItem.IsVisible, Is.True);
            Assert.That(checkoutBranchMenuItem.Header, Is.EqualTo("Checkout branch 'feature/menu'"));
            Assert.That(checkoutBranch, Is.EqualTo("feature/menu"));
        });
    }

    [Test]
    public void ContextMenuOpening_RowWithOnlyCurrentLocalBranch_HidesCheckoutBranchAction()
    {
        var objectId = ObjectId.Parse("0123456789abcdef0123456789abcdef01234567");
        var revision = new GitRevision(objectId)
        {
            Refs = [new GitRef(null!, objectId, "refs/heads/main")]
        };
        var grid = new RevisionDataGrid
        {
            CurrentBranchName = "main"
        };
        grid.LoadRevisions([new RevisionRow(revision, graphRow: null, prevRow: null, nextRow: null)]);
        GetCommitList(grid).SelectedIndex = 0;

        InvokePrivate(grid, "CommitContextMenu_Opening", GetContextMenu(grid), new CancelEventArgs());

        Assert.That(GetMenuItem(grid, "CheckoutBranchMenuItem").IsVisible, Is.False);
    }

    [Test]
    public void ContextMenuOpening_RowWithRemoteBranch_ShowsCheckoutRemoteBranchAction()
    {
        var objectId = ObjectId.Parse("0123456789abcdef0123456789abcdef01234567");
        var revision = new GitRevision(objectId)
        {
            Refs = [new GitRef(null!, objectId, "refs/remotes/origin/feature/menu", "origin")]
        };
        var grid = new RevisionDataGrid();
        grid.LoadRevisions([new RevisionRow(revision, graphRow: null, prevRow: null, nextRow: null)]);
        GetCommitList(grid).SelectedIndex = 0;

        InvokePrivate(grid, "CommitContextMenu_Opening", GetContextMenu(grid), new CancelEventArgs());

        string? checkoutRemoteBranch = null;
        grid.CheckoutRemoteBranchRequested += branch => checkoutRemoteBranch = branch;
        MenuItem checkoutRemoteBranchMenuItem = GetMenuItem(grid, "CheckoutRemoteBranchMenuItem");
        InvokePrivate(grid, "CtxCheckoutRemoteBranch_Click", checkoutRemoteBranchMenuItem, null);

        Assert.Multiple(() =>
        {
            Assert.That(checkoutRemoteBranchMenuItem.IsVisible, Is.True);
            Assert.That(checkoutRemoteBranchMenuItem.Header, Is.EqualTo("Checkout remote branch as local 'origin/feature/menu'"));
            Assert.That(checkoutRemoteBranch, Is.EqualTo("origin/feature/menu"));
        });
    }

    [Test]
    public void ContextMenuOpening_RowWithBranchAndTagRefs_ShowsWindowsStyleRefActions()
    {
        var objectId = ObjectId.Parse("0123456789abcdef0123456789abcdef01234567");
        var revision = new GitRevision(objectId)
        {
            Refs =
            [
                new GitRef(null!, objectId, "refs/heads/main"),
                new GitRef(null!, objectId, "refs/heads/feature/menu"),
                new GitRef(null!, objectId, "refs/remotes/origin/feature/menu", "origin"),
                new GitRef(null!, objectId, "refs/tags/v1.2.3")
            ]
        };
        var grid = new RevisionDataGrid
        {
            CurrentBranchName = "main",
            CurrentHeadHash = "ffffffffffffffffffffffffffffffffffffffff"
        };
        grid.LoadRevisions([new RevisionRow(revision, graphRow: null, prevRow: null, nextRow: null)]);
        GetCommitList(grid).SelectedIndex = 0;

        InvokePrivate(grid, "CommitContextMenu_Opening", GetContextMenu(grid), new CancelEventArgs());

        string? mergeRef = null;
        string? rebaseRef = null;
        string? deleteBranch = null;
        string? deleteRemoteBranch = null;
        string? deleteTag = null;
        string? pushBranch = null;
        string? createBranchAtRef = null;
        grid.MergeRefRequested += value => mergeRef = value;
        grid.RebaseRefRequested += value => rebaseRef = value;
        grid.DeleteBranchRequested += value => deleteBranch = value;
        grid.DeleteRemoteBranchRequested += value => deleteRemoteBranch = value;
        grid.DeleteTagRequested += value => deleteTag = value;
        grid.PushBranchRequested += value => pushBranch = value;
        grid.CreateBranchAtRefRequested += value => createBranchAtRef = value;

        MenuItem mergeMenuItem = GetMenuItem(grid, "MergeRefMenuItem");
        MenuItem rebaseMenuItem = GetMenuItem(grid, "RebaseRefMenuItem");
        MenuItem deleteBranchMenuItem = GetMenuItem(grid, "DeleteBranchMenuItem");
        MenuItem deleteRemoteMenuItem = GetMenuItem(grid, "DeleteRemoteBranchMenuItem");
        MenuItem deleteTagMenuItem = GetMenuItem(grid, "DeleteTagMenuItem");
        MenuItem pushMenuItem = GetMenuItem(grid, "PushBranchMenuItem");
        MenuItem createBranchAtRefMenuItem = GetMenuItem(grid, "CreateBranchAtRefMenuItem");
        MenuItem copyRefNameMenuItem = GetMenuItem(grid, "CopyRefNameMenuItem");

        InvokePrivate(grid, "CtxMergeRef_Click", GetChildMenuItem(mergeMenuItem, "v1.2.3"), null);
        InvokePrivate(grid, "CtxRebaseRef_Click", GetChildMenuItem(rebaseMenuItem, "origin/feature/menu"), null);
        InvokePrivate(grid, "CtxDeleteBranch_Click", deleteBranchMenuItem, null);
        InvokePrivate(grid, "CtxDeleteRemoteBranch_Click", deleteRemoteMenuItem, null);
        InvokePrivate(grid, "CtxDeleteTag_Click", deleteTagMenuItem, null);
        InvokePrivate(grid, "CtxPushBranch_Click", GetChildMenuItem(pushMenuItem, "main"), null);
        InvokePrivate(grid, "CtxCreateBranchAtRef_Click", GetChildMenuItem(createBranchAtRefMenuItem, "v1.2.3"), null);

        Assert.Multiple(() =>
        {
            Assert.That(mergeMenuItem.IsVisible, Is.True);
            Assert.That(mergeMenuItem.Items.Count, Is.EqualTo(3));
            Assert.That(rebaseMenuItem.IsVisible, Is.True);
            Assert.That(rebaseMenuItem.Items.Count, Is.EqualTo(2));
            Assert.That(deleteBranchMenuItem.Header, Is.EqualTo("Delete branch 'feature/menu'"));
            Assert.That(deleteRemoteMenuItem.Header, Is.EqualTo("Delete remote branch 'origin/feature/menu'"));
            Assert.That(deleteTagMenuItem.Header, Is.EqualTo("Delete tag 'v1.2.3'"));
            Assert.That(pushMenuItem.Items.Count, Is.EqualTo(3));
            Assert.That(createBranchAtRefMenuItem.Items.Count, Is.EqualTo(4));
            Assert.That(copyRefNameMenuItem.Items.Count, Is.EqualTo(4));
            Assert.That(mergeRef, Is.EqualTo("v1.2.3"));
            Assert.That(rebaseRef, Is.EqualTo("origin/feature/menu"));
            Assert.That(deleteBranch, Is.EqualTo("feature/menu"));
            Assert.That(deleteRemoteBranch, Is.EqualTo("origin/feature/menu"));
            Assert.That(deleteTag, Is.EqualTo("v1.2.3"));
            Assert.That(pushBranch, Is.EqualTo("main"));
            Assert.That(createBranchAtRef, Is.EqualTo("v1.2.3"));
        });
    }

    [Test]
    public void ContextMenuOpening_CurrentHeadRow_HidesMergeAndRebaseRefActions()
    {
        var objectId = ObjectId.Parse("0123456789abcdef0123456789abcdef01234567");
        var revision = new GitRevision(objectId)
        {
            Refs =
            [
                new GitRef(null!, objectId, "refs/heads/feature/menu"),
                new GitRef(null!, objectId, "refs/remotes/origin/feature/menu", "origin")
            ]
        };
        var grid = new RevisionDataGrid
        {
            CurrentBranchName = "main",
            CurrentHeadHash = objectId.ToString()
        };
        grid.LoadRevisions([new RevisionRow(revision, graphRow: null, prevRow: null, nextRow: null)]);
        GetCommitList(grid).SelectedIndex = 0;

        InvokePrivate(grid, "CommitContextMenu_Opening", GetContextMenu(grid), new CancelEventArgs());

        Assert.Multiple(() =>
        {
            Assert.That(GetMenuItem(grid, "MergeRefMenuItem").IsVisible, Is.False);
            Assert.That(GetMenuItem(grid, "RebaseRefMenuItem").IsVisible, Is.False);
            Assert.That(GetMenuItem(grid, "DeleteBranchMenuItem").IsVisible, Is.True);
        });
    }

    private static ListBox GetCommitList(RevisionDataGrid grid)
        => grid.FindControl<ListBox>("CommitList")
           ?? throw new InvalidOperationException("RevisionDataGrid CommitList was not loaded.");

    private static ContextMenu GetContextMenu(RevisionDataGrid grid)
        => GetCommitList(grid).ContextMenu
           ?? throw new InvalidOperationException("RevisionDataGrid context menu was not loaded.");

    private static MenuItem GetMenuItem(RevisionDataGrid grid, string name)
        => grid.FindControl<MenuItem>(name)
           ?? throw new InvalidOperationException($"RevisionDataGrid menu item {name} was not loaded.");

    private static MenuItem GetChildMenuItem(MenuItem parent, string header)
        => parent.Items.OfType<MenuItem>().FirstOrDefault(item => string.Equals(item.Header?.ToString(), header, StringComparison.Ordinal))
           ?? throw new InvalidOperationException($"Menu item {parent.Name} does not contain child {header}.");

    private static void InvokePrivate(object target, string methodName, params object?[] parameters)
    {
        MethodInfo? method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
        method!.Invoke(target, parameters);
    }
}
