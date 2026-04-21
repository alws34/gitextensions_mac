using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class PullDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public PullDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var remotes = await _module.GetRemotesAsync();
        string currentBranch = await Task.Run(() => _module.GitExecutable.GetOutput("branch --show-current").Trim());
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            RemoteComboBox.ItemsSource = remotes.Select(r => r.Name).ToList();
            if (RemoteComboBox.Items.Count > 0)
            {
                RemoteComboBox.SelectedIndex = 0;
            }

            BranchTextBox.Text = currentBranch;
        });
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        _ = PullAsync();
    }

    private async Task PullAsync()
    {
        string remote = RemoteComboBox.SelectedItem?.ToString() ?? string.Empty;
        string branch = BranchTextBox.Text?.Trim() ?? string.Empty;

        string strategy = RebaseRadio.IsChecked == true ? "--rebase"
            : FetchOnlyRadio.IsChecked == true ? "--fetch-only"
            : string.Empty;

        string args = $"pull {strategy} {remote} {branch}".Trim();

        await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = "Pulling...");
        string result = await Task.Run(() => _module.GitExecutable.GetOutput(args));
        await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = result.Trim());
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
