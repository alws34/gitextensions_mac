using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;

namespace GitExtensions.Plugins.DeleteUnusedBranches;

public partial class DeleteUnusedBranchesAvaloniaForm : Window
{
    private readonly GitModule _module;
    private List<string> _branches = [];

    public DeleteUnusedBranchesAvaloniaForm(GitModule module)
    {
        _module = module;
        InitializeComponent();
    }

    private void Search_Click(object? sender, RoutedEventArgs e)
    {
        _ = SearchAsync();
    }

    private async Task SearchAsync()
    {
        string baseBranch = BaseBranchBox.Text ?? "origin/HEAD";
        bool includeRemotes = IncludeRemotesCheck.IsChecked == true;

        _branches = await Task.Run(() =>
        {
            string flag = includeRemotes ? "-a" : string.Empty;
            string output = _module.GitExecutable.GetOutput($"branch {flag} --merged {baseBranch}");
            return output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(b => b.Trim().TrimStart('*').Trim())
                .Where(b => b.Length > 0 &&
                            !b.Equals("master", StringComparison.OrdinalIgnoreCase) &&
                            !b.Equals("main", StringComparison.OrdinalIgnoreCase) &&
                            !b.Equals("develop", StringComparison.OrdinalIgnoreCase) &&
                            !b.StartsWith("HEAD", StringComparison.OrdinalIgnoreCase))
                .ToList();
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            BranchesList.ItemsSource = new List<string>(_branches);
            StatusLabel.Text = $"{_branches.Count} branches found";
        });
    }

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        _ = DeleteAsync();
    }

    private async Task DeleteAsync()
    {
        var selected = BranchesList.SelectedItems?
            .OfType<string>()
            .ToList() ?? [];

        if (selected.Count == 0)
        {
            return;
        }

        int deleted = 0;
        await Task.Run(() =>
        {
            foreach (string branch in selected)
            {
                string flag = branch.StartsWith("remotes/", StringComparison.OrdinalIgnoreCase) ? "-dr" : "-d";
                _module.GitExecutable.GetOutput($"branch {flag} {branch}");
                deleted++;
            }
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusLabel.Text = $"Deleted {deleted} branches.";
        });

        await SearchAsync();
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
}
