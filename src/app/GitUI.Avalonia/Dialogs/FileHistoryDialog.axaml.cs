using Avalonia.Controls;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class FileHistoryDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private readonly string _filePath;
    private List<string> _hashes = [];

    public FileHistoryDialog(GitModule module, string filePath)
    {
        _module = module;
        _filePath = filePath;
        InitializeComponent();
        Title = $"File History — {System.IO.Path.GetFileName(filePath)}";
        _ = LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        var (hashes, labels) = await Task.Run(() =>
        {
            string output = _module.GitExecutable.GetOutput($"log --oneline -- \"{_filePath}\"");
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var hs = lines.Select(l => l.Split(' ')[0]).ToList();
            return (hs, lines.ToList());
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _hashes = hashes;
            CommitsList.ItemsSource = labels;
        });
    }

    private void CommitsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        int idx = CommitsList.SelectedIndex;
        if (idx < 0 || idx >= _hashes.Count)
        {
            return;
        }

        _ = LoadFileAtRevisionAsync(_hashes[idx]);
    }

    private async Task LoadFileAtRevisionAsync(string hash)
    {
        string content = await Task.Run(() =>
            _module.GitExecutable.GetOutput($"show {hash}:\"{_filePath}\""));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            FileEditor.Text = content;
        });
    }
}
