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
        UserNameBox.Text = App.Settings.GetString("userName", string.Empty);
        UserEmailBox.Text = App.Settings.GetString("userEmail", string.Empty);
        _ = DetectGitVersionAsync();
    }

    public void SaveSettings()
    {
        App.Settings.SetString("gitBinDir", GitPathTextBox.Text?.Trim() ?? "git");
        App.Settings.SetString("userName", UserNameBox.Text?.Trim() ?? string.Empty);
        App.Settings.SetString("userEmail", UserEmailBox.Text?.Trim() ?? string.Empty);
    }

    private async Task DetectGitVersionAsync()
    {
        try
        {
            string git = GitPathTextBox.Text?.Trim() is { Length: > 0 } p ? p : "git";
            string version = await Task.Run(() =>
            {
                var psi = new System.Diagnostics.ProcessStartInfo(git, "--version")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                };
                using var proc = System.Diagnostics.Process.Start(psi);
                return proc?.StandardOutput.ReadToEnd().Trim() ?? string.Empty;
            });
            await Dispatcher.UIThread.InvokeAsync(() =>
                TestResultLabel.Text = string.IsNullOrEmpty(version) ? "git not found" : version);
        }
        catch
        {
            await Dispatcher.UIThread.InvokeAsync(() => TestResultLabel.Text = "git not found");
        }
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
            _ = DetectGitVersionAsync();
        }
    }

    private void Test_Click(object? sender, RoutedEventArgs e)
    {
        _ = DetectGitVersionAsync();
    }
}
