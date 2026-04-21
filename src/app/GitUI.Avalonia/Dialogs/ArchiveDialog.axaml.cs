using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class ArchiveDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public ArchiveDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
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

        var files = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save archive as",
        });
        if (files is not null)
        {
            await Dispatcher.UIThread.InvokeAsync(() => OutputPathTextBox.Text = files.Path.LocalPath);
        }
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        _ = ArchiveAsync();
    }

    private async Task ArchiveAsync()
    {
        string outputPath = OutputPathTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(outputPath))
        {
            await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = "Please enter an output path.");
            return;
        }

        string format = (FormatCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "zip";
        string commit = CommitTextBox.Text?.Trim() ?? string.Empty;
        string commitArg = string.IsNullOrEmpty(commit) ? "HEAD" : commit;

        string result = await Task.Run(() =>
            _module.GitExecutable.GetOutput($"archive --format={format} {commitArg} -o \"{outputPath}\""));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusLabel.Text = string.IsNullOrWhiteSpace(result) ? "Archive created." : result.Trim();
        });

        if (!result.Contains("error") && !result.Contains("fatal"))
        {
            Close(true);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
