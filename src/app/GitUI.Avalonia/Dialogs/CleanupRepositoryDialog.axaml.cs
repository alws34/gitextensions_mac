using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class CleanupRepositoryDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public CleanupRepositoryDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        _ = CleanAsync();
    }

    private async Task CleanAsync()
    {
        bool dryRun = DryRunCheckBox.IsChecked == true;
        bool dirs = UntrackedDirsCheckBox.IsChecked == true;
        bool ignored = IgnoredFilesCheckBox.IsChecked == true;

        string flags = dryRun ? "-n" : "-f";
        if (dirs)
        {
            flags += "d";
        }

        if (ignored)
        {
            flags += "x";
        }

        string result = await Task.Run(() => _module.GitExecutable.GetOutput($"clean {flags}"));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ResultLabel.Text = string.IsNullOrWhiteSpace(result) ? "Nothing to clean." : result.Trim();
        });

        if (!dryRun && !result.Contains("error"))
        {
            Close(true);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
