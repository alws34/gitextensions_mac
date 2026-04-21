using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class RemotesDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private IReadOnlyList<Remote> _remotes = [];

    public RemotesDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        _ = LoadRemotesAsync();
    }

    private async Task LoadRemotesAsync()
    {
        _remotes = await _module.GetRemotesAsync();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            RemotesList.ItemsSource = _remotes.Select(r => r.Name).ToList();
        });
    }

    private void Remote_Selected(object? sender, SelectionChangedEventArgs e)
    {
        int idx = RemotesList.SelectedIndex;
        if (idx < 0 || idx >= _remotes.Count)
        {
            return;
        }

        Remote r = _remotes[idx];
        RemoteNameTextBox.Text = r.Name;
        RemoteUrlTextBox.Text = r.FetchUrl;
    }

    private void SaveRemote_Click(object? sender, RoutedEventArgs e)
    {
        _ = SaveRemoteAsync();
    }

    private async Task SaveRemoteAsync()
    {
        string name = RemoteNameTextBox.Text?.Trim() ?? string.Empty;
        string url = RemoteUrlTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
        {
            return;
        }

        await Task.Run(() =>
        {
            int idx = RemotesList.SelectedIndex;
            if (idx >= 0 && idx < _remotes.Count)
            {
                string oldName = _remotes[idx].Name;
                _module.RenameRemote(oldName, name);
                _module.GitExecutable.GetOutput($"remote set-url {name} {url}");
            }
            else
            {
                _module.AddRemote(name, url);
            }
        });

        await LoadRemotesAsync();
    }

    private void AddRemote_Click(object? sender, RoutedEventArgs e)
    {
        RemotesList.SelectedIndex = -1;
        RemoteNameTextBox.Text = string.Empty;
        RemoteUrlTextBox.Text = string.Empty;
    }

    private void RemoveRemote_Click(object? sender, RoutedEventArgs e)
    {
        _ = RemoveRemoteAsync();
    }

    private async Task RemoveRemoteAsync()
    {
        int idx = RemotesList.SelectedIndex;
        if (idx < 0 || idx >= _remotes.Count)
        {
            return;
        }

        string name = _remotes[idx].Name;
        await Task.Run(() => _module.RemoveRemote(name));
        await LoadRemotesAsync();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
}
