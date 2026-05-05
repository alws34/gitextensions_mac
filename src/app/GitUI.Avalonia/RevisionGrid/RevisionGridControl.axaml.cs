using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility;
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
    public event Action<RevisionRow?>? SelectedRowChanged;
    public event Action<string>? CheckoutHashRequested;
    public event Action<string>? CheckoutBranchRequested;
    public event Action<string>? CheckoutRemoteBranchRequested;
    public event Action<string>? CherryPickHashRequested;
    public event Action<string>? RevertHashRequested;
    public event Action<string>? CreateBranchAtHashRequested;
    public event Action<string>? CreateTagAtHashRequested;
    public event Action<string>? ResetHardToHashRequested;
    public event Action<string>? InteractiveRebaseRequested;

    private List<RevisionRow> _allRows = [];

    public RevisionGridControl()
    {
        InitializeComponent();
        DataGrid.SelectedRevisionChanged += rev => SelectedRevisionChanged?.Invoke(rev);
        DataGrid.SelectedRowChanged += row => SelectedRowChanged?.Invoke(row);
        DataGrid.CheckoutHashRequested += hash => CheckoutHashRequested?.Invoke(hash);
        DataGrid.CheckoutBranchRequested += branch => CheckoutBranchRequested?.Invoke(branch);
        DataGrid.CheckoutRemoteBranchRequested += remoteBranch => CheckoutRemoteBranchRequested?.Invoke(remoteBranch);
        DataGrid.CherryPickHashRequested += hash => CherryPickHashRequested?.Invoke(hash);
        DataGrid.RevertHashRequested += hash => RevertHashRequested?.Invoke(hash);
        DataGrid.CreateBranchAtHashRequested += hash => CreateBranchAtHashRequested?.Invoke(hash);
        DataGrid.CreateTagAtHashRequested += hash => CreateTagAtHashRequested?.Invoke(hash);
        DataGrid.ResetHardToHashRequested += hash => ResetHardToHashRequested?.Invoke(hash);
        DataGrid.InteractiveRebaseRequested += hash => InteractiveRebaseRequested?.Invoke(hash);
    }

    protected override void OnModuleSet()
    {
        _ = LoadRevisionsAsync();
    }

    public Task RefreshAsync() => LoadRevisionsAsync();

    public void ScrollToHash(string shortHash) => DataGrid.ScrollToHash(shortHash);

    public void NavigateParent() => DataGrid.SelectRelative(1);

    public void NavigateChild() => DataGrid.SelectRelative(-1);

    public void NavigateBack() => DataGrid.SelectRelative(-1);

    public void NavigateForward() => DataGrid.SelectRelative(1);

    public void SetFilter(string text, Controls.FilterType type)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            DataGrid.LoadRevisions(_allRows);
            return;
        }

        var filtered = _allRows.Where(r => type switch
        {
            Controls.FilterType.Author => r.Author.Contains(text, StringComparison.OrdinalIgnoreCase),
            Controls.FilterType.Hash => r.ShortHash.StartsWith(text, StringComparison.OrdinalIgnoreCase),
            Controls.FilterType.Message => r.Subject.Contains(text, StringComparison.OrdinalIgnoreCase),
            _ => r.Author.Contains(text, StringComparison.OrdinalIgnoreCase)
                 || r.Subject.Contains(text, StringComparison.OrdinalIgnoreCase)
                 || r.ShortHash.StartsWith(text, StringComparison.OrdinalIgnoreCase),
        }).ToList();

        DataGrid.LoadRevisions(filtered);
    }

    public async System.Threading.Tasks.Task SetFilterAsync(string text, Controls.FilterType type)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            await LoadRevisionsAsync();
            return;
        }

        if (type == Controls.FilterType.Author)
        {
            await LoadRevisionsAsync($"--author=\"{text}\"");
        }
        else if (type == Controls.FilterType.Message)
        {
            await LoadRevisionsAsync($"--grep=\"{text}\"");
        }
        else
        {
            // Hash and All: client-side filter on cached rows
            SetFilter(text, type);
        }
    }

    private async System.Threading.Tasks.Task LoadRevisionsAsync(string extraArgs = "")
    {
        if (Module is null)
        {
            return;
        }

        try
        {
            // --- Artificial rows: Working Tree + Index ---
            string statusOutput = string.Empty;
            string headHash = string.Empty;
            string currentBranch = string.Empty;
            try
            {
                statusOutput = await Module.GitExecutable.GetOutputAsync("status --porcelain");
                headHash = (await Module.GitExecutable.GetOutputAsync("rev-parse HEAD")).Trim();
                currentBranch = Module.GetCurrentBranchName();
            }
            catch
            {
                // non-fatal — repo might be empty
            }

            var statusLines = statusOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            int stagedCount = statusLines.Count(l => l.Length >= 2 && l[0] != ' ' && l[0] != '?');
            int unstagedCount = statusLines.Count(l => l.Length >= 2 && (l[1] != ' ' || l[0] == '?'));

            var workTreeRow = new RevisionRow(
                "0000000000000000000000000000000000000002",
                unstagedCount > 0 ? $"Working directory ({unstagedCount} files)" : "Working directory",
                "WorkTree");

            var indexRow = new RevisionRow(
                "0000000000000000000000000000000000000001",
                stagedCount > 0 ? $"Index ({stagedCount} staged files)" : "Index",
                "Index");

            // --- Regular git log rows ---
            int maxRevisions = Math.Clamp(App.Settings.GetInt("revisionGridMaxRevisions", MaxRevisions), 100, 50000);
            string firstParentArg = App.Settings.GetBool("showFirstParentOnly", false) ? " --first-parent" : string.Empty;
            string output = await Module.GitExecutable.GetOutputAsync(
                $"log --format={LogFormat}%n --max-count={maxRevisions}{firstParentArg}{(string.IsNullOrEmpty(extraArgs) ? string.Empty : " " + extraArgs)}");

            var gitRevisions = ParseGitLog(output);

            // Assign branch/tag refs to each commit so badges render in the grid
            try
            {
                RefsFilter refsFilter = RefsFilter.Heads | RefsFilter.Remotes;
                if (App.Settings.GetBool("showTags", true))
                {
                    refsFilter |= RefsFilter.Tags;
                }

                var allRefs = await System.Threading.Tasks.Task.Run(
                    () => Module.GetRefs(refsFilter));
                var refsByGuid = allRefs
                    .Where(r => !string.IsNullOrEmpty(r.Guid))
                    .GroupBy(r => r.Guid!)
                    .ToDictionary(g => g.Key, g => (IReadOnlyList<IGitRef>)g.ToArray());
                foreach (GitRevision rev in gitRevisions)
                {
                    if (!string.IsNullOrEmpty(rev.Guid) && refsByGuid.TryGetValue(rev.Guid, out var revRefs))
                    {
                        rev.Refs = revRefs;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadRefs failed: {ex.Message}");
            }

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

            var rows = new List<RevisionRow>(count + 2);

            // Artificial rows first
            rows.Add(workTreeRow);
            rows.Add(indexRow);

            for (int i = 0; i < count; i++)
            {
                RevisionGraphRevision? node = graph.GetNodeForRow(i);
                if (node?.GitRevision is not null)
                {
                    bool isCurrent = !string.IsNullOrEmpty(headHash)
                        && node.GitRevision.Guid == headHash;
                    rows.Add(new RevisionRow(
                        node.GitRevision,
                        graph.GetSegmentsForRow(i),
                        i > 0 ? graph.GetSegmentsForRow(i - 1) : null,
                        i < count - 1 ? graph.GetSegmentsForRow(i + 1) : null)
                    {
                        IsCurrent = isCurrent
                    });
                }
            }

            _allRows = rows;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                DataGrid.CurrentBranchName = currentBranch;
                DataGrid.LoadRevisions(rows);
            });
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
