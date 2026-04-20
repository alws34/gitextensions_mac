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
        FileList.ItemsSource = files.ToList();
    }

    private void FileList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectedFileChanged?.Invoke(FileList.SelectedItem as FileStatusItem);
    }
}
