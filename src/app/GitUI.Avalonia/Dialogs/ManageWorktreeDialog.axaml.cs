using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class ManageWorktreeDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private List<WorktreeInfo> _worktrees = [];

    public ManageWorktreeDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        _ = LoadWorktreesAsync();
    }

    private async Task LoadWorktreesAsync()
    {
        _worktrees = await Task.Run(() =>
        {
            string output = _module.GitExecutable.GetOutput("worktree list --porcelain");
            return ParseWorktrees(output);
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            WorktreesGrid.ItemsSource = _worktrees
                .Select(w => $"{w.Path}  [{w.Branch}]  {w.Head}")
                .ToList();
        });
    }

    private static List<WorktreeInfo> ParseWorktrees(string porcelainOutput)
    {
        var result = new List<WorktreeInfo>();
        string path = string.Empty;
        string head = string.Empty;
        string branch = string.Empty;

        foreach (string line in porcelainOutput.Split('\n'))
        {
            if (line.StartsWith("worktree "))
            {
                if (!string.IsNullOrEmpty(path))
                {
                    result.Add(new WorktreeInfo(path, branch, head));
                }

                path = line[9..];
                head = string.Empty;
                branch = string.Empty;
            }
            else if (line.StartsWith("HEAD "))
            {
                head = line[5..][..7];
            }
            else if (line.StartsWith("branch refs/heads/"))
            {
                branch = line[18..];
            }
            else if (line == "detached")
            {
                branch = "(detached)";
            }
        }

        if (!string.IsNullOrEmpty(path))
        {
            result.Add(new WorktreeInfo(path, branch, head));
        }

        return result;
    }

    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        _ = AddWorktreeAsync();
    }

    private async Task AddWorktreeAsync()
    {
        var parent = TopLevel.GetTopLevel(this) as Window;
        if (parent is null)
        {
            return;
        }

        var dialog = new CreateWorktreeDialog(_module);
        bool? result = await dialog.ShowDialog<bool?>(parent);
        if (result == true)
        {
            await LoadWorktreesAsync();
        }
    }

    private void Remove_Click(object? sender, RoutedEventArgs e)
    {
        _ = RemoveWorktreeAsync();
    }

    private async Task RemoveWorktreeAsync()
    {
        if (WorktreesGrid.SelectedIndex < 0 || WorktreesGrid.SelectedIndex >= _worktrees.Count)
        {
            return;
        }

        WorktreeInfo selected = _worktrees[WorktreesGrid.SelectedIndex];
        await Task.Run(() => _module.GitExecutable.GetOutput($"worktree remove \"{selected.Path}\""));
        await LoadWorktreesAsync();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private sealed record WorktreeInfo(string Path, string Branch, string Head);
}
