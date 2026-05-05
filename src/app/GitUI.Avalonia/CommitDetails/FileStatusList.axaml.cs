using System.Diagnostics;
using System.IO;
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
    public event Action<string>? StatusRequested;
    public event Action<string>? ErrorOccurred;

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
        FileStatusItem? file = SelectedFile;
        bool hasFile = file is not null;
        string? fullPath = GetWorkingTreePath(file);
        bool fileExists = fullPath is not null && File.Exists(fullPath);
        bool directoryExists = fullPath is not null && Directory.Exists(Path.GetDirectoryName(fullPath));

        OpenDiffMenuItem.IsEnabled = hasFile;
        OpenFileMenuItem.IsEnabled = fileExists;
        RevealInFinderMenuItem.IsEnabled = fileExists;
        OpenContainingFolderMenuItem.IsEnabled = directoryExists;
        CopyPathMenuItem.IsEnabled = hasFile;
        CopyFullPathMenuItem.IsEnabled = fullPath is not null;
        CopyFileNameMenuItem.IsEnabled = hasFile;
        HistoryMenuItem.IsEnabled = hasFile;
        BlameMenuItem.IsEnabled = hasFile;

        if (!hasFile)
        {
            e.Cancel = true;
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

    private void CtxOpenFile_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetWorkingTreePath(SelectedFile) is { } fullPath && File.Exists(fullPath))
        {
            StartOpenProcess(fullPath);
        }
    }

    private void CtxRevealInFinder_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetWorkingTreePath(SelectedFile) is not { } fullPath || !File.Exists(fullPath))
        {
            return;
        }

        try
        {
            var psi = new ProcessStartInfo { FileName = "open", UseShellExecute = false };
            psi.ArgumentList.Add("-R");
            psi.ArgumentList.Add(fullPath);
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex.Message);
        }
    }

    private void CtxOpenContainingFolder_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetWorkingTreePath(SelectedFile) is not { } fullPath)
        {
            return;
        }

        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
        {
            StartOpenProcess(directory);
        }
    }

    private void CtxCopyPath_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (SelectedFile?.Name is { } path)
        {
            _ = CopyTextAsync(path, "Copied file path");
        }
    }

    private void CtxCopyFullPath_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (GetWorkingTreePath(SelectedFile) is { } fullPath)
        {
            _ = CopyTextAsync(fullPath, "Copied full path");
        }
    }

    private void CtxCopyFileName_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (SelectedFile?.Name is { } path)
        {
            _ = CopyTextAsync(Path.GetFileName(path), "Copied file name");
        }
    }

    private string? GetWorkingTreePath(FileStatusItem? file)
    {
        if (file is null || Module is null)
        {
            return null;
        }

        return Path.IsPathRooted(file.Name)
            ? file.Name
            : Path.GetFullPath(Path.Combine(Module.WorkingDir, file.Name));
    }

    private async System.Threading.Tasks.Task CopyTextAsync(string text, string statusMessage)
    {
        try
        {
            await (TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(text)
                ?? System.Threading.Tasks.Task.CompletedTask);
            StatusRequested?.Invoke(statusMessage);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex.Message);
        }
    }

    private void StartOpenProcess(string path)
    {
        try
        {
            Process.Start("open", path);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex.Message);
        }
    }
}
