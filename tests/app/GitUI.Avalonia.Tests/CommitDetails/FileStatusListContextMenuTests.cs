using System.ComponentModel;
using System.Reflection;
using Avalonia.Controls;
using GitUI.Avalonia.CommitDetails;
using NUnit.Framework;

namespace GitUI.Avalonia.Tests.CommitDetails;

[TestFixture]
public class FileStatusListContextMenuTests
{
    [SetUp]
    public void SetUp()
    {
        AvaloniaTestHost.EnsureStarted();
    }

    [Test]
    public void ContextMenuOpening_NoSelectedFile_DisablesFileActions()
    {
        var list = new FileStatusList();

        CancelEventArgs args = InvokeContextMenuOpening(list);

        Assert.Multiple(() =>
        {
            Assert.That(args.Cancel, Is.True);
            Assert.That(GetCommandMenuItems(list).Select(item => item.IsEnabled), Is.All.False);
        });
    }

    [Test]
    public void ContextMenuOpening_SelectedDirectory_DisablesFileActionsAndRaisesNullSelection()
    {
        var list = new FileStatusList();
        list.LoadFiles([new FileStatusItem("src/Foo.cs", IsAdded: false, IsDeleted: false, IsRenamed: false)]);
        FileTreeNode directory = GetRootNodes(list).Single();
        FileStatusItem? selectedFile = new("previous.txt", false, false, false);
        list.SelectedFileChanged += item => selectedFile = item;

        SelectNode(list, directory);
        CancelEventArgs args = InvokeContextMenuOpening(list);

        Assert.Multiple(() =>
        {
            Assert.That(args.Cancel, Is.True);
            Assert.That(list.SelectedFile, Is.Null);
            Assert.That(selectedFile, Is.Null);
            Assert.That(GetCommandMenuItems(list).Select(item => item.IsEnabled), Is.All.False);
        });
    }

    [Test]
    public void ContextMenuOpening_SelectedFile_EnablesFileActionsAndRaisesPathEvents()
    {
        var list = new FileStatusList();
        var file = new FileStatusItem("src/Foo.cs", IsAdded: false, IsDeleted: false, IsRenamed: false);
        list.LoadFiles([file]);
        FileTreeNode fileNode = GetRootNodes(list).Single().Children.Single();

        string? historyPath = null;
        string? blamePath = null;
        FileStatusItem? selectedFile = null;
        list.HistoryRequested += path => historyPath = path;
        list.BlameRequested += path => blamePath = path;
        list.SelectedFileChanged += item => selectedFile = item;

        SelectNode(list, fileNode);
        CancelEventArgs args = InvokeContextMenuOpening(list);
        InvokePrivate(list, "CtxHistory_Click", null, null);
        InvokePrivate(list, "CtxBlame_Click", null, null);

        Assert.Multiple(() =>
        {
            Assert.That(args.Cancel, Is.False);
            Assert.That(list.SelectedFile, Is.SameAs(file));
            Assert.That(selectedFile, Is.SameAs(file));
            Assert.That(
                GetCommandMenuItems(list).Select(item => item.IsEnabled),
                Is.EqualTo(new[]
                {
                    true,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    true,
                    false,
                    true,
                    true,
                    true,
                    false,
                    false,
                    false,
                }));
            Assert.That(historyPath, Is.EqualTo(file.Name));
            Assert.That(blamePath, Is.EqualTo(file.Name));
        });
    }

    private static IReadOnlyList<FileTreeNode> GetRootNodes(FileStatusList list)
        => GetFileTree(list).ItemsSource as IReadOnlyList<FileTreeNode>
           ?? throw new InvalidOperationException("FileStatusList root nodes were not loaded.");

    private static TreeView GetFileTree(FileStatusList list)
        => list.FindControl<TreeView>("FileTree")
           ?? throw new InvalidOperationException("FileStatusList FileTree was not loaded.");

    private static IReadOnlyList<MenuItem> GetCommandMenuItems(FileStatusList list)
    {
        ContextMenu menu = GetFileTree(list).ContextMenu
            ?? throw new InvalidOperationException("FileStatusList context menu was not loaded.");

        return [.. menu.Items.OfType<MenuItem>()];
    }

    private static CancelEventArgs InvokeContextMenuOpening(FileStatusList list)
    {
        var args = new CancelEventArgs();
        InvokePrivate(list, "FileTree_ContextMenuOpening", GetFileTree(list), args);
        return args;
    }

    private static void SelectNode(FileStatusList list, FileTreeNode node)
    {
        TreeView fileTree = GetFileTree(list);
        fileTree.SelectedItem = node;
        InvokePrivate(list, "FileTree_SelectionChanged", fileTree, null);
    }

    private static void InvokePrivate(object target, string methodName, params object?[] parameters)
    {
        MethodInfo? method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
        method!.Invoke(target, parameters);
    }
}
