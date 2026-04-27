using Avalonia.Controls;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.CommitDetails;

public record FileStatusItem(string Name, bool IsAdded, bool IsDeleted, bool IsRenamed)
{
    public string StatusIcon => (IsAdded, IsDeleted, IsRenamed) switch
    {
        (true, _, _) => "A",
        (_, true, _) => "D",
        (_, _, true) => "R",
        _ => "M",
    };
}

public partial class FileStatusList : GitModuleControl
{
    public event Action<FileStatusItem?>? SelectedFileChanged;
    public event Action<string>? BlameRequested;
    public event Action<string>? HistoryRequested;

    private FileTreeNode? _selectedNode;

    public FileStatusItem? SelectedFile => _selectedNode?.FileItem;

    public FileStatusList() => InitializeComponent();

    public void LoadFiles(IEnumerable<FileStatusItem> files)
    {
        FileTree.ItemsSource = FileTreeNode.BuildTree(files);
    }

    private void FileTree_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _selectedNode = FileTree.SelectedItem as FileTreeNode;
        SelectedFileChanged?.Invoke(_selectedNode?.FileItem);
    }

    private void FileTree_ContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        bool hasFile = SelectedFile is not null;
        if (FileTree.ContextMenu is { } menu)
        {
            foreach (var item in menu.Items.OfType<MenuItem>())
            {
                item.IsEnabled = hasFile;
            }
        }
    }

    private void CtxDiff_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Diff is already shown via SelectionChanged
    }

    private void CtxHistory_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (SelectedFile?.Name is { } path)
        {
            HistoryRequested?.Invoke(path);
        }
    }

    private void CtxBlame_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (SelectedFile?.Name is { } path)
        {
            BlameRequested?.Invoke(path);
        }
    }
}
