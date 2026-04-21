using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class StashDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private List<string> _stashes = [];

    public StashDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        _ = LoadStashesAsync();
    }

    private async Task LoadStashesAsync()
    {
        string output = await Task.Run(() => _module.GitExecutable.GetOutput("stash list"));
        _stashes = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            StashList.ItemsSource = _stashes;
        });
    }

    private void StashSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (StashList.SelectedIndex < 0)
        {
            return;
        }

        _ = ShowStashDiffAsync(StashList.SelectedIndex);
    }

    private async Task ShowStashDiffAsync(int index)
    {
        string stashRef = $"stash@{{{index}}}";
        string diff = await Task.Run(() => _module.GitExecutable.GetOutput($"stash show -p {stashRef}"));
        await Dispatcher.UIThread.InvokeAsync(() => DiffView.Text = diff);
    }

    private void Stash_Click(object? sender, RoutedEventArgs e)
    {
        _ = StashSaveAsync();
    }

    private async Task StashSaveAsync()
    {
        await Task.Run(() => _module.GitExecutable.GetOutput("stash push"));
        await LoadStashesAsync();
    }

    private void Apply_Click(object? sender, RoutedEventArgs e)
    {
        _ = StashApplyAsync();
    }

    private async Task StashApplyAsync()
    {
        int idx = StashList.SelectedIndex;
        if (idx < 0)
        {
            return;
        }

        await Task.Run(() => _module.GitExecutable.GetOutput($"stash apply stash@{{{idx}}}"));
    }

    private void Pop_Click(object? sender, RoutedEventArgs e)
    {
        _ = StashPopAsync();
    }

    private async Task StashPopAsync()
    {
        int idx = StashList.SelectedIndex;
        if (idx < 0)
        {
            return;
        }

        await Task.Run(() => _module.GitExecutable.GetOutput($"stash pop stash@{{{idx}}}"));
        await LoadStashesAsync();
    }

    private void Drop_Click(object? sender, RoutedEventArgs e)
    {
        _ = StashDropAsync();
    }

    private async Task StashDropAsync()
    {
        int idx = StashList.SelectedIndex;
        if (idx < 0)
        {
            return;
        }

        await Task.Run(() => _module.GitExecutable.GetOutput($"stash drop stash@{{{idx}}}"));
        await LoadStashesAsync();
    }
}
