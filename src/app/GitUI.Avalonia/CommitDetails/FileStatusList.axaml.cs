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

    public FileStatusList() => InitializeComponent();

    public void LoadFiles(IEnumerable<FileStatusItem> files)
    {
        FileTree.ItemsSource = FileTreeNode.BuildTree(files);
    }

    private void FileTree_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        FileStatusItem? item = (FileTree.SelectedItem as FileTreeNode)?.FileItem;
        SelectedFileChanged?.Invoke(item);
    }
}
