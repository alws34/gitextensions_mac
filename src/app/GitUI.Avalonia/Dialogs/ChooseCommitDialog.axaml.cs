using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class ChooseCommitDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private List<string> _hashes = [];
    private List<string> _labels = [];

    public string? SelectedHash { get; private set; }

    public ChooseCommitDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        _ = LoadCommitsAsync(string.Empty);
    }

    private async Task LoadCommitsAsync(string filter)
    {
        var (hashes, labels) = await Task.Run(() =>
        {
            string args = "log --oneline --max-count=200";
            if (!string.IsNullOrWhiteSpace(filter))
            {
                args += $" --grep=\"{filter}\"";
            }

            string output = _module.GitExecutable.GetOutput(args);
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var hs = lines.Select(l => l.Split(' ')[0]).ToList();
            return (hs, lines.ToList());
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _hashes = hashes;
            _labels = labels;
            CommitsList.ItemsSource = labels;
        });
    }

    private void SearchBox_KeyUp(object? sender, KeyEventArgs e)
    {
        _ = LoadCommitsAsync(SearchBox.Text ?? string.Empty);
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        int idx = CommitsList.SelectedIndex;
        if (idx >= 0 && idx < _hashes.Count)
        {
            SelectedHash = _hashes[idx];
        }

        Close(SelectedHash);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
}
