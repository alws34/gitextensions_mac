using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class ViewPatchDialog : GitExtensionsDialog
{
    public ViewPatchDialog()
    {
        InitializeComponent();
    }

    private void Open_Click(object? sender, RoutedEventArgs e)
    {
        _ = OpenAsync();
    }

    private async Task OpenAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open patch file",
            FileTypeFilter = [new FilePickerFileType("Patch files") { Patterns = ["*.patch", "*.diff"] }],
        });

        if (files.Count == 0)
        {
            return;
        }

        string path = files[0].Path.LocalPath;
        await Dispatcher.UIThread.InvokeAsync(() => PatchFileTextBox.Text = path);

        string content = await System.Threading.Tasks.Task.Run(() => System.IO.File.ReadAllText(path));
        await Dispatcher.UIThread.InvokeAsync(() => PatchViewer.Text = content);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
}
