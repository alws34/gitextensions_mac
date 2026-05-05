using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitExtUtils;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class PushDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private readonly string? _defaultBranch;

    public PushDialog(GitModule module, string? defaultBranch = null)
    {
        _module = module;
        _defaultBranch = defaultBranch;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var remotes = await _module.GetRemotesAsync();
        string currentBranch = await Task.Run(() => _module.GitExecutable.GetOutput("branch --show-current").Trim());
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var remoteNames = remotes.Select(r => r.Name).ToList();
            string branch = string.IsNullOrWhiteSpace(_defaultBranch)
                ? currentBranch
                : _defaultBranch;
            string? defaultRemote = null;
            foreach (string remoteName in remoteNames)
            {
                string prefix = remoteName + "/";
                if (branch.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    defaultRemote = remoteName;
                    branch = branch[prefix.Length..];
                    break;
                }
            }

            RemoteComboBox.ItemsSource = remoteNames;
            if (remoteNames.Count > 0)
            {
                int remoteIndex = defaultRemote is null
                    ? -1
                    : remoteNames.FindIndex(remote => string.Equals(remote, defaultRemote, StringComparison.OrdinalIgnoreCase));
                RemoteComboBox.SelectedIndex = remoteIndex >= 0 ? remoteIndex : 0;
            }

            BranchTextBox.Text = branch;
        });
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        _ = PushAsync();
    }

    private async Task PushAsync()
    {
        string remote = RemoteComboBox.SelectedItem?.ToString() ?? string.Empty;
        string branch = BranchTextBox.Text?.Trim() ?? string.Empty;
        string forceFlag = ForceCheckBox.IsChecked == true ? "--force-with-lease " : string.Empty;

        string args = $"push {forceFlag}{remote.Quote()} {branch.Quote()}".Trim();

        await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = "Pushing...");
        string result = await Task.Run(() => _module.GitExecutable.GetOutput(args));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusLabel.Text = result.Trim();
            bool failed = result.Contains("error:", StringComparison.OrdinalIgnoreCase)
                || result.Contains("rejected", StringComparison.OrdinalIgnoreCase)
                || result.Contains("fatal:", StringComparison.OrdinalIgnoreCase);
            if (!failed)
            {
                Close(true);
            }
        });
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
