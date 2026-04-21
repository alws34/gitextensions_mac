using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

/// <summary>
/// Dialog for cloning a remote repository to a local destination.
/// </summary>
public partial class CloneDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public CloneDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
    }

    private void BrowseDest_Click(object? sender, RoutedEventArgs e)
    {
        _ = BrowseDestAsync();
    }

    private async Task BrowseDestAsync()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select destination"
        });

        if (folders.Count > 0)
        {
            await Dispatcher.UIThread.InvokeAsync(() => DestPathTextBox.Text = folders[0].Path.LocalPath);
        }
    }

    private void Clone_Click(object? sender, RoutedEventArgs e)
    {
        _ = DoCloneAsync();
    }

    private async Task DoCloneAsync()
    {
        string url = UrlTextBox.Text?.Trim() ?? string.Empty;
        string dest = DestPathTextBox.Text?.Trim() ?? string.Empty;
        string branch = BranchTextBox.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(url))
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusTextBlock.Text = "Please enter a source URL.";
                StatusTextBlock.IsVisible = true;
            });
            return;
        }

        if (string.IsNullOrEmpty(dest))
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusTextBlock.Text = "Please enter a destination path.";
                StatusTextBlock.IsVisible = true;
            });
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusTextBlock.Text = "Cloning...";
            StatusTextBlock.IsVisible = true;
        });

        string result = await Task.Run(() =>
        {
            string args = string.IsNullOrEmpty(branch)
                ? $"clone \"{url}\" \"{dest}\""
                : $"clone --branch \"{branch}\" \"{url}\" \"{dest}\"";

            return _module.GitExecutable.GetOutput(args);
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusTextBlock.Text = string.IsNullOrWhiteSpace(result) ? "Clone completed." : result;
            StatusTextBlock.IsVisible = true;
        });
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
