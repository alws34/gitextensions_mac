using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class ReflogDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private List<ReflogEntry> _entries = [];

    public ReflogDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        _ = LoadReflogAsync();
    }

    private async Task LoadReflogAsync()
    {
        _entries = await Task.Run(() =>
        {
            string output = _module.GitExecutable.GetOutput("reflog --format=%h|%gs");
            return output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                         .Select(line =>
                         {
                             int pipe = line.IndexOf('|');
                             if (pipe < 0)
                             {
                                 return new ReflogEntry(line, string.Empty, string.Empty);
                             }

                             string hash = line[..pipe].Trim();
                             string action = line[(pipe + 1)..].Trim();
                             int colon = action.IndexOf(':');
                             string msg = colon >= 0 ? action[(colon + 2)..].Trim() : string.Empty;
                             if (colon >= 0)
                             {
                                 action = action[..colon].Trim();
                             }

                             return new ReflogEntry(hash, action, msg);
                         })
                         .ToList();
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ReflogGrid.ItemsSource = _entries
                .Select(e => $"{e.Hash}  {e.Action}: {e.Message}")
                .ToList();
        });
    }

    private void Checkout_Click(object? sender, RoutedEventArgs e)
    {
        int idx = ReflogGrid.SelectedIndex;
        if (idx >= 0 && idx < _entries.Count)
        {
            _ = Task.Run(() => _module.GitExecutable.GetOutput($"checkout {_entries[idx].Hash}"));
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private sealed record ReflogEntry(string Hash, string Action, string Message);
}
