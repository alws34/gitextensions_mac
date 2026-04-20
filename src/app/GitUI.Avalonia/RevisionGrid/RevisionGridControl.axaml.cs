using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Base;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.RevisionGrid;

public partial class RevisionGridControl : GitModuleControl
{
    private const string LogFormat = "%H%n%an%n%ae%n%ai%n%s";
    private const int MaxRevisions = 2000;

    public event Action<GitRevision?>? SelectedRevisionChanged;

    public RevisionGridControl()
    {
        InitializeComponent();
        DataGrid.SelectedRevisionChanged += rev => SelectedRevisionChanged?.Invoke(rev);
    }

    protected override void OnModuleSet()
    {
        _ = LoadRevisionsAsync();
    }

    private async System.Threading.Tasks.Task LoadRevisionsAsync()
    {
        if (Module is null)
        {
            return;
        }

        var revisions = await System.Threading.Tasks.Task.Run(() => ParseGitLog(Module));

        await Dispatcher.UIThread.InvokeAsync(() => DataGrid.LoadRevisions(revisions));
    }

    private static IReadOnlyList<GitRevision> ParseGitLog(GitCommands.GitModule module)
    {
        string output = module.GitExecutable.GetOutput(
            $"log --format={LogFormat}%n --max-count={MaxRevisions}");

        var revisions = new List<GitRevision>();
        var lines = output.Split('\n');

        for (int i = 0; i + 4 < lines.Length; i += 6)
        {
            string hash = lines[i].Trim();
            if (!ObjectId.TryParse(hash, out var objectId))
            {
                continue;
            }

            var rev = new GitRevision(objectId)
            {
                Author = lines[i + 1].Trim(),
                AuthorEmail = lines[i + 2].Trim(),
                Subject = lines[i + 4].Trim(),
            };

            if (DateTime.TryParse(lines[i + 3].Trim(), out var dt))
            {
                rev.AuthorUnixTime = ((DateTimeOffset)dt).ToUnixTimeSeconds();
            }

            revisions.Add(rev);
        }

        return revisions;
    }
}
