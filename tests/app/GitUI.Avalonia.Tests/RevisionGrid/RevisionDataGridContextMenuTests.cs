using System.ComponentModel;
using System.Reflection;
using Avalonia.Controls;
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

    private static ListBox GetCommitList(RevisionDataGrid grid)
        => grid.FindControl<ListBox>("CommitList")
           ?? throw new InvalidOperationException("RevisionDataGrid CommitList was not loaded.");

    private static ContextMenu GetContextMenu(RevisionDataGrid grid)
        => GetCommitList(grid).ContextMenu
           ?? throw new InvalidOperationException("RevisionDataGrid context menu was not loaded.");

    private static void InvokePrivate(object target, string methodName, params object?[] parameters)
    {
        MethodInfo? method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
        method!.Invoke(target, parameters);
    }
}
