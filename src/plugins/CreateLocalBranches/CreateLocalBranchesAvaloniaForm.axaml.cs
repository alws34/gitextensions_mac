using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;

namespace GitExtensions.Plugins.CreateLocalBranches;

public partial class CreateLocalBranchesAvaloniaForm : Window
{
    private readonly GitModule _module;
    private List<string> _remoteBranches = [];

    public CreateLocalBranchesAvaloniaForm(GitModule module)
    {
        _module = module;
        InitializeComponent();
        _ = LoadBranchesAsync();
    }

    private async Task LoadBranchesAsync()
    {
        _remoteBranches = await Task.Run(() =>
        {
            string output = _module.GitExecutable.GetOutput("branch -r");
            return output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(b => b.Trim())
                .Where(b => !b.Contains("->"))
                .ToList();
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            BranchesList.ItemsSource = new List<string>(_remoteBranches);
        });
    }

    private void Create_Click(object? sender, RoutedEventArgs e)
    {
        _ = CreateAsync();
    }

    private async Task CreateAsync()
    {
        var selected = BranchesList.SelectedItems?
            .OfType<string>()
            .ToList() ?? [];

        int created = 0;
        await Task.Run(() =>
        {
            foreach (string remote in selected)
            {
                string local = remote.Contains('/') ? remote[(remote.IndexOf('/') + 1)..] : remote;
                _module.GitExecutable.GetOutput($"branch --track {local} {remote}");
                created++;
            }
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusLabel.Text = $"Created {created} branches.";
        });
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
}
