using Avalonia.Controls;
using Avalonia.Threading;
using GitCommands;
using GitExtUtils;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.CommitDetails;

public partial class CommitDetailsPanel : UserControl
{
    private GitModule? _module;
    private GitRevision? _currentRevision;
    private bool _diffLoaded;
    private bool _fileListEventsAttached;

    public event Action? RepositoryChanged;

    public CommitDetailsPanel() => InitializeComponent();

    public void SetModule(GitModule module)
    {
        _module = module;
        Summary.Module = module;
        DiffView.Module = module;
        FileList.Module = module;
        AttachFileListEvents();
    }

    public void ApplySettings()
    {
        DiffView.ApplySettings();
    }

    private void AttachFileListEvents()
    {
        if (_fileListEventsAttached)
        {
            return;
        }

        _fileListEventsAttached = true;
        FileList.SelectedFileChanged += OnFileSelected;
        FileList.BlameRequested += OnBlameRequested;
        FileList.HistoryRequested += OnHistoryRequested;
        FileList.StatusRequested += message => System.Diagnostics.Debug.WriteLine(message);
        FileList.ErrorOccurred += message => System.Diagnostics.Debug.WriteLine($"File action failed: {message}");
        FileList.AddToGitIgnoreRequested += OnAddToGitIgnoreRequested;
        FileList.UserScriptsRequested += OnUserScriptsRequested;
        FileList.RepositoryChanged += () => RepositoryChanged?.Invoke();
    }

    public async System.Threading.Tasks.Task ShowRevisionRowAsync(RevisionGrid.RevisionRow? row)
    {
        if (row?.Revision is { } revision)
        {
            await ShowRevisionAsync(revision);
            return;
        }

        _currentRevision = null;
        _diffLoaded = false;
        Summary.ShowRevision(null);
        Summary.ShowBody(row?.Subject ?? string.Empty);

        if (_module is null)
        {
            FileList.LoadFiles([]);
            return;
        }

        var files = await System.Threading.Tasks.Task.Run(() =>
            LoadArtificialFiles(row?.ArtificialType ?? string.Empty));
        FileList.LoadFiles(files);
    }

    public async System.Threading.Tasks.Task ShowRevisionAsync(GitRevision? revision)
    {
        _currentRevision = revision;
        _diffLoaded = false;
        Summary.ShowRevision(revision);

        if (revision is null || _module is null)
        {
            return;
        }

        var (files, body) = await System.Threading.Tasks.Task.Run(() =>
        {
            List<FileStatusItem> fileList;
            try
            {
                fileList = _module.GetDiffFilesWithUntracked(
                    revision.Guid + "^",
                    revision.Guid,
                    GitExtensions.Extensibility.Git.StagedStatus.None)
                    .Select(f => new FileStatusItem(
                        f.Name,
                        f.IsAdded,
                        f.IsDeleted,
                        f.IsRenamed,
                        IsStaged: false,
                        IsUnstaged: true,
                        IsSubmodule: f.IsSubmodule))
                    .ToList();
            }
            catch
            {
                fileList = [];
            }

            string commitBody = string.Empty;
            try
            {
                commitBody = _module.GitExecutable.GetOutput($"log -1 --format=%b {revision.Guid}").Trim();
            }
            catch
            {
                // body is optional, ignore failures
            }

            return (fileList, commitBody);
        });

        Summary.ShowBody(body);
        FileList.LoadFiles(files);

        if (DetailsTabs.SelectedIndex == 0)
        {
            _diffLoaded = true;
            await DiffView.ShowDiffAsync(revision);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Avalonia event handler")]
    private async void DetailsTabs_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_currentRevision is null)
        {
            return;
        }

        if (DetailsTabs.SelectedIndex == 0 && !_diffLoaded)
        {
            _diffLoaded = true;
            await DiffView.ShowDiffAsync(_currentRevision);
        }
        else if (DetailsTabs.SelectedIndex == 1)
        {
            await LoadCommitTreeAsync(_currentRevision);
        }
    }

    private async System.Threading.Tasks.Task LoadCommitTreeAsync(GitRevision revision)
    {
        if (_module is null)
        {
            return;
        }

        string output = await _module.GitExecutable.GetOutputAsync(
            $"ls-tree -r --name-only {revision.Guid}");

        var paths = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                          .Select(p => p.Trim())
                          .Where(p => p.Length > 0)
                          .ToList();

        var nodes = CommitTreeNode.BuildTree(paths);
        await Dispatcher.UIThread.InvokeAsync(() => CommitTreeView.ItemsSource = nodes);
    }

    private void OnFileSelected(FileStatusItem? file)
    {
        if (file is null || _currentRevision is null)
        {
            return;
        }

        _ = DiffView.ShowDiffAsync(_currentRevision, file.Name);
    }

    private void OnBlameRequested(string path)
    {
        if (_module is { } module)
        {
            new Dialogs.BlameDialog(module, path).Show();
        }
    }

    private void OnHistoryRequested(string path)
    {
        if (_module is { } module)
        {
            new Dialogs.FileHistoryDialog(module, path).Show();
        }
    }

    private void OnAddToGitIgnoreRequested(string path)
    {
        if (_module is { } module)
        {
            new Dialogs.AddToGitIgnoreDialog(module, path).Show();
        }
    }

    private void OnUserScriptsRequested(string path)
    {
        if (_module is { } module)
        {
            new Dialogs.ScriptsManagerDialog(module).Show();
        }
    }

    private List<FileStatusItem> LoadArtificialFiles(string artificialType)
    {
        if (_module is null)
        {
            return [];
        }

        try
        {
            string output = _module.GitExecutable.GetOutput("status --porcelain=v1");
            return [.. output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(ParseStatusLine)
                .Where(item => item is not null)
                .Select(item => item!)
                .Where(item => artificialType switch
                {
                    "Index" => item.IsStaged,
                    "WorkTree" => item.IsUnstaged,
                    _ => true,
                })];
        }
        catch
        {
            return [];
        }
    }

    private static FileStatusItem? ParseStatusLine(string line)
    {
        if (line.Length < 4)
        {
            return null;
        }

        char indexStatus = line[0];
        char workTreeStatus = line[1];
        string path = line[3..].Trim();
        int renameSeparator = path.IndexOf(" -> ", StringComparison.Ordinal);
        if (renameSeparator >= 0)
        {
            path = path[(renameSeparator + 4)..];
        }

        path = path.Trim('"').ToPosixPath();
        bool isAdded = indexStatus == 'A' || workTreeStatus == 'A' || indexStatus == '?';
        bool isDeleted = indexStatus == 'D' || workTreeStatus == 'D';
        bool isRenamed = indexStatus == 'R' || workTreeStatus == 'R';
        bool isStaged = indexStatus != ' ' && indexStatus != '?';
        bool isUnstaged = workTreeStatus != ' ' || indexStatus == '?';

        return new FileStatusItem(
            path,
            isAdded,
            isDeleted,
            isRenamed,
            isStaged,
            isUnstaged,
            IsSubmodule: indexStatus == 'M' && workTreeStatus == 'M');
    }
}
