using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class FormatPatchDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public FormatPatchDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
    }

    private void BrowseDir_Click(object? sender, RoutedEventArgs e)
    {
        _ = BrowseDirAsync();
    }

    private async Task BrowseDirAsync()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select output directory",
        });
        if (folders.Count > 0)
        {
            await Dispatcher.UIThread.InvokeAsync(() => OutputDirTextBox.Text = folders[0].Path.LocalPath);
        }
    }

    private void Format_Click(object? sender, RoutedEventArgs e)
    {
        _ = FormatAsync();
    }

    private async Task FormatAsync()
    {
        string from = FromTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(from))
        {
            await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = "Please enter a from commit.");
            return;
        }

        string to = ToTextBox.Text?.Trim() ?? string.Empty;
        string range = string.IsNullOrEmpty(to) ? from : $"{from}..{to}";
        string outputDir = OutputDirTextBox.Text?.Trim() ?? string.Empty;
        string outputArg = string.IsNullOrEmpty(outputDir) ? string.Empty : $" -o \"{outputDir}\"";

        string result = await Task.Run(() => _module.GitExecutable.GetOutput($"format-patch {range}{outputArg}"));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusLabel.Text = string.IsNullOrWhiteSpace(result) ? "Done." : result.Trim();
        });
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
