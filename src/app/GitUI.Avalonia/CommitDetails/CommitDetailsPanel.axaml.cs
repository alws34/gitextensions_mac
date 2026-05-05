using Avalonia.Controls;
using Avalonia.Threading;
using GitCommands;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.CommitDetails;

public partial class CommitDetailsPanel : UserControl
{
    private GitModule? _module;
    private GitRevision? _currentRevision;
    private bool _diffLoaded;

    public CommitDetailsPanel() => InitializeComponent();

    public void SetModule(GitModule module)
    {
        _module = module;
        Summary.Module = module;
        DiffView.Module = module;
        FileList.Module = module;
        FileList.SelectedFileChanged += OnFileSelected;
        FileList.BlameRequested += path =>
        {
            new Dialogs.BlameDialog(module, path).Show();
        };
        FileList.HistoryRequested += path =>
        {
            new Dialogs.FileHistoryDialog(module, path).Show();
        };
        FileList.StatusRequested += message =>
        {
            System.Diagnostics.Debug.WriteLine(message);
        };
        FileList.ErrorOccurred += message =>
        {
            System.Diagnostics.Debug.WriteLine($"File action failed: {message}");
        };
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
                    .Select(f => new FileStatusItem(f.Name, f.IsAdded, f.IsDeleted, f.IsRenamed))
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
        else if (DetailsTabs.SelectedIndex == 2)
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
}
