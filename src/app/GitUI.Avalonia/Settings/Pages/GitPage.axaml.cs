using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GitUI.Avalonia.Settings;

namespace GitUI.Avalonia.Settings.Pages;

public partial class GitPage : UserControl, ISettingsPage
{
    public GitPage()
    {
        InitializeComponent();
        GitPathTextBox.Text = App.Settings.GetString("gitBinDir", "git");
    }

    public void SaveSettings()
    {
        App.Settings.SetString("gitBinDir", GitPathTextBox.Text ?? "git");
    }

    private void Browse_Click(object? sender, RoutedEventArgs e)
    {
        _ = BrowseAsync();
    }

    private async Task BrowseAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select git executable",
        });
        if (files.Count > 0)
        {
            GitPathTextBox.Text = files[0].Path.LocalPath;
        }
    }

    private void Test_Click(object? sender, RoutedEventArgs e)
    {
        _ = TestGitAsync();
    }

    private async Task TestGitAsync()
    {
        try
        {
            string result = await Task.Run(() =>
            {
                using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = GitPathTextBox.Text ?? "git",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                });
                return process?.StandardOutput.ReadToEnd() ?? "Failed to start git";
            });
            await Dispatcher.UIThread.InvokeAsync(() => TestResultLabel.Text = result.Trim());
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => TestResultLabel.Text = $"Error: {ex.Message}");
        }
    }
}
