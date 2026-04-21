using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class InitDialog : GitExtensionsDialog
{
    public InitDialog()
    {
        InitializeComponent();
    }

    private void Browse_Click(object? sender, RoutedEventArgs e)
    {
        _ = BrowseAsync();
    }

    private async Task BrowseAsync()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select directory to initialize",
        });
        if (folders.Count > 0)
        {
            await Dispatcher.UIThread.InvokeAsync(() => PathTextBox.Text = folders[0].Path.LocalPath);
        }
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        string path = PathTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(path))
        {
            await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = "Please enter a path.");
            return;
        }

        string result = await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"init \"{path}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                using var p = System.Diagnostics.Process.Start(psi);
                string stdout = p?.StandardOutput.ReadToEnd() ?? string.Empty;
                string stderr = p?.StandardError.ReadToEnd() ?? string.Empty;
                return string.IsNullOrEmpty(stderr) ? stdout : stderr;
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        });

        await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = result.Trim());
        if (!result.Contains("error") && !result.Contains("Error"))
        {
            Close(true);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
