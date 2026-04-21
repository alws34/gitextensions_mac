using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class UpdatesDialog : GitExtensionsDialog
{
    public UpdatesDialog()
    {
        InitializeComponent();
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        CurrentVersionLabel.Text = $"Current version: {version?.Major}.{version?.Minor}.{version?.Build}";
        _ = CheckForUpdatesAsync();
    }

    private void Check_Click(object? sender, RoutedEventArgs e)
    {
        _ = CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            LatestVersionLabel.Text = "Checking...";
            StatusLabel.Text = string.Empty;
        });

        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "GitExtensions");
            string json = await client.GetStringAsync(
                "https://api.github.com/repos/gitextensions/gitextensions/releases/latest");
            int tagStart = json.IndexOf("\"tag_name\":\"", StringComparison.Ordinal);
            string latestVersion = "unknown";
            if (tagStart >= 0)
            {
                tagStart += 12;
                int tagEnd = json.IndexOf('"', tagStart);
                latestVersion = json[tagStart..tagEnd].TrimStart('v');
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LatestVersionLabel.Text = $"Latest version: {latestVersion}";
                StatusLabel.Text = "Visit https://github.com/gitextensions/gitextensions/releases to download.";
            });
        }
        catch
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LatestVersionLabel.Text = "Could not check for updates.";
            });
        }
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close(null);
}
