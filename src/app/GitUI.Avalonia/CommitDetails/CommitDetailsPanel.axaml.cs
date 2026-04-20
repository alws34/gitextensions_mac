using Avalonia.Controls;
using GitCommands;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.CommitDetails;

public partial class CommitDetailsPanel : UserControl
{
    private GitModule? _module;
    private GitRevision? _currentRevision;

    public CommitDetailsPanel() => InitializeComponent();

    public void SetModule(GitModule module)
    {
        _module = module;
        Summary.Module = module;
        DiffView.Module = module;
        FileList.Module = module;
        FileList.SelectedFileChanged += OnFileSelected;
    }

    public async System.Threading.Tasks.Task ShowRevisionAsync(GitRevision? revision)
    {
        _currentRevision = revision;
        Summary.ShowRevision(revision);

        if (revision is null || _module is null)
        {
            return;
        }

        var files = await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                return _module.GetDiffFilesWithUntracked(
                    revision.Guid + "^",
                    revision.Guid,
                    GitExtensions.Extensibility.Git.StagedStatus.None)
                    .Select(f => new FileStatusItem(f.Name, f.IsAdded, f.IsDeleted, f.IsRenamed))
                    .ToList();
            }
            catch
            {
                return [];
            }
        });

        FileList.LoadFiles(files);
        await DiffView.ShowDiffAsync(revision);
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
