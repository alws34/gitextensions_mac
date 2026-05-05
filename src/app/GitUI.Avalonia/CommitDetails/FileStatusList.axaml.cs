using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using GitCommands;
using GitExtUtils;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.CommitDetails;

public enum FileListViewMode
{
    Tree,
    Flat,
    Grouped,
}

public record FileStatusItem(
    string Name,
    bool IsAdded,
    bool IsDeleted,
    bool IsRenamed,
    bool IsStaged = false,
    bool IsUnstaged = false,
    bool IsSubmodule = false)
{
    public string StatusIcon => (IsAdded, IsDeleted, IsRenamed) switch
    {
        (true, _, _) => "A",
        (_, true, _) => "D",
        (_, _, true) => "R",
        _ => "M",
    };

    public string StatusGroup => (IsStaged, IsUnstaged) switch
    {
        (true, true) => "Staged and unstaged",
        (true, false) => "Staged",
        (false, true) => "Unstaged",
        _ => "Changed",
    };

    public bool CanStage => IsUnstaged || (!IsStaged && !IsDeleted);
    public bool CanUnstage => IsStaged;
    public bool CanReset => IsUnstaged || IsDeleted || IsAdded;
}

public partial class FileStatusList : GitModuleControl
{
    public event Action<FileStatusItem?>? SelectedFileChanged;
    public event Action<string>? BlameRequested;
    public event Action<string>? HistoryRequested;
    public event Action<string>? StatusRequested;
    public event Action<string>? ErrorOccurred;
    public event Action<string>? AddToGitIgnoreRequested;
    public event Action<string>? UserScriptsRequested;
    public event Action? RepositoryChanged;

    private FileTreeNode? _selectedNode;
    private List<FileStatusItem> _files = [];
    private FileListViewMode _viewMode = FileListViewMode.Tree;
    private bool _suppressViewModeChanged;

    public FileStatusItem? SelectedFile => _selectedNode?.FileItem;

    public FileStatusList()
    {
        InitializeComponent();
        _suppressViewModeChanged = true;
        _viewMode = GetInitialViewMode();
        ViewModeComboBox.SelectedIndex = (int)_viewMode;
        _suppressViewModeChanged = false;
    }

    private static FileListViewMode GetInitialViewMode()
    {
        if (App.Settings is null)
        {
            return FileListViewMode.Tree;
        }

        return App.Settings.GetString("fileListViewMode", "tree") switch
        {
            "flat" => FileListViewMode.Flat,
            "grouped" => FileListViewMode.Grouped,
            _ => FileListViewMode.Tree,
        };
    }

    public void LoadFiles(IEnumerable<FileStatusItem> files)
    {
        _files = [.. files];
        ReloadFiles();
    }

    private void FileTree_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _selectedNode = FileTree.SelectedItem as FileTreeNode;
        if (_selectedNode?.FileItem is { } file)
        {
            SelectedFileChanged?.Invoke(file);
        }
    }

    private void FileTree_ContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        FileStatusItem? file = SelectedFile;
        bool hasFile = file is not null;
        string? fullPath = GetWorkingTreePath(file);
        bool fileExists = fullPath is not null && File.Exists(fullPath);
        bool directoryExists = fullPath is not null && Directory.Exists(Path.GetDirectoryName(fullPath));
        bool hasModule = Module is not null;

        OpenDiffMenuItem.IsEnabled = hasFile;
        StageFileMenuItem.IsEnabled = hasFile && hasModule && file!.CanStage;
        UnstageFileMenuItem.IsEnabled = hasFile && hasModule && file!.CanUnstage;
        ResetFileMenuItem.IsEnabled = hasFile && hasModule && file!.CanReset;
        OpenFileMenuItem.IsEnabled = fileExists;
        RevealInFinderMenuItem.IsEnabled = fileExists;
        OpenContainingFolderMenuItem.IsEnabled = directoryExists;
        OpenWithDifftoolMenuItem.IsEnabled = hasFile && hasModule;
        CopyPathMenuItem.IsEnabled = hasFile;
        CopyFullPathMenuItem.IsEnabled = fullPath is not null;
        CopyFileNameMenuItem.IsEnabled = hasFile;
        HistoryMenuItem.IsEnabled = hasFile;
        BlameMenuItem.IsEnabled = hasFile;
        UpdateSubmoduleMenuItem.IsEnabled = hasFile && hasModule && file!.IsSubmodule;
        AddToGitIgnoreMenuItem.IsEnabled = hasFile && hasModule;
        UserScriptsMenuItem.IsEnabled = hasFile && hasModule;

        if (!hasFile)
        {
            e.Cancel = true;
        }
    }

    private void ViewModeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressViewModeChanged)
        {
            return;
        }

        _viewMode = ViewModeComboBox.SelectedIndex switch
        {
            1 => FileListViewMode.Flat,
            2 => FileListViewMode.Grouped,
            _ => FileListViewMode.Tree,
        };
        App.Settings.SetString("fileListViewMode", _viewMode.ToString().ToLowerInvariant());
        App.Settings.Save();
        ReloadFiles();
    }

    private void ReloadFiles()
    {
        FileTree.ItemsSource = _viewMode switch
        {
            FileListViewMode.Flat => FileTreeNode.BuildFlat(_files),
            FileListViewMode.Grouped => FileTreeNode.BuildGrouped(_files),
            _ => FileTreeNode.BuildTree(_files),
        };
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

    private void CtxStageFile_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _ = RunGitFileActionAsync("Stage file", "add --", reloadAfter: true);
    }

    private void CtxUnstageFile_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _ = RunGitFileActionAsync("Unstage file", "reset HEAD --", reloadAfter: true);
    }

    private void CtxResetFile_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _ = RunGitFileActionAsync("Reset file", "checkout --", reloadAfter: true);
    }

    private void CtxOpenWithDifftool_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _ = RunGitFileActionAsync("Open with difftool", "difftool --", reloadAfter: false);
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

    private void CtxUpdateSubmodule_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _ = RunGitFileActionAsync("Update submodule", "submodule update --init --", reloadAfter: true);
    }

    private void CtxAddToGitIgnore_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (SelectedFile?.Name is { } path)
        {
            AddToGitIgnoreRequested?.Invoke(path);
        }
    }

    private void CtxUserScripts_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (SelectedFile?.Name is { } path)
        {
            UserScriptsRequested?.Invoke(path);
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

    private async System.Threading.Tasks.Task RunGitFileActionAsync(string label, string commandPrefix, bool reloadAfter)
    {
        if (Module is null || SelectedFile?.Name is not { } path)
        {
            return;
        }

        try
        {
            string gitPath = path.ToPosixPath().QuoteNE() ?? path.Quote();
            string output = await Module.GitExecutable.GetOutputAsync($"{commandPrefix} {gitPath}");
            string status = string.IsNullOrWhiteSpace(output)
                ? $"{label}: {path}"
                : output.Trim();
            StatusRequested?.Invoke(status);
            if (reloadAfter)
            {
                RepositoryChanged?.Invoke();
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex.Message);
        }
    }
}
