using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Base;
using GitUI.Avalonia.RevisionGrid.Graph;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.RevisionGrid;

public partial class RevisionGridControl : GitModuleControl
{
    // Format: hash, parents (space-sep), author, email, date, subject — blank line between commits
    private const string LogFormat = "%H%n%P%n%an%n%ae%n%ai%n%s";
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

        try
        {
            string output = await Module.GitExecutable.GetOutputAsync(
                $"log --format={LogFormat}%n --max-count={MaxRevisions}");

            var gitRevisions = ParseGitLog(output);

            var graph = new RevisionGraph();
            foreach (GitRevision rev in gitRevisions)
            {
                graph.Add(rev);
            }

            graph.LoadingCompleted();

            int count = graph.Count;
            if (count > 0)
            {
                graph.CacheTo(count - 1, count - 1);
            }

            var rows = new List<RevisionRow>(count);
            for (int i = 0; i < count; i++)
            {
                RevisionGraphRevision? node = graph.GetNodeForRow(i);
                if (node?.GitRevision is not null)
                {
                    rows.Add(new RevisionRow(node.GitRevision, graph.GetSegmentsForRow(i)));
                }
            }

            await Dispatcher.UIThread.InvokeAsync(() => DataGrid.LoadRevisions(rows));
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => DataGrid.LoadRevisions([]));
            System.Diagnostics.Debug.WriteLine($"LoadRevisionsAsync failed: {ex}");
            await Console.Error.WriteLineAsync($"LoadRevisionsAsync failed: {ex}");
        }
    }

    private static IReadOnlyList<GitRevision> ParseGitLog(string output)
    {
        var revisions = new List<GitRevision>();
        var lines = output.Split('\n');

        // Each commit block: hash, parents, author, email, date, subject, blank  (7 lines)
        for (int i = 0; i + 5 < lines.Length; i += 7)
        {
            string hash = lines[i].Trim();
            if (!ObjectId.TryParse(hash, out var objectId))
            {
                continue;
            }

            var rev = new GitRevision(objectId)
            {
                Author = lines[i + 2].Trim(),
                AuthorEmail = lines[i + 3].Trim(),
                Subject = lines[i + 5].Trim(),
            };

            // Parse parent hashes
            string parentsLine = lines[i + 1].Trim();
            if (!string.IsNullOrEmpty(parentsLine))
            {
                rev.ParentIds = parentsLine
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => ObjectId.TryParse(p, out var pid) ? pid : null)
                    .Where(pid => pid is not null)
                    .ToList()!;
            }

            if (DateTime.TryParse(lines[i + 4].Trim(), out var dt))
            {
                rev.AuthorUnixTime = ((DateTimeOffset)dt).ToUnixTimeSeconds();
            }

            revisions.Add(rev);
        }

        return revisions;
    }
}
