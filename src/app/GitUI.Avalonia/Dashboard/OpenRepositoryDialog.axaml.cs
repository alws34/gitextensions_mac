using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dashboard;

public partial class OpenRepositoryDialog : GitExtensionsDialog
{
    public OpenRepositoryDialog() => InitializeComponent();

    private void Browse_Click(object? sender, RoutedEventArgs e)
    {
        _ = BrowseAsync();
    }

    private async System.Threading.Tasks.Task BrowseAsync()
    {
        var result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Repository Folder",
            AllowMultiple = false,
        });

        if (result.Count > 0)
        {
            PathBox.Text = result[0].Path.LocalPath;
        }
    }

    private void Open_Click(object? sender, RoutedEventArgs e)
    {
        var path = PathBox.Text?.Trim();
        if (!string.IsNullOrEmpty(path))
        {
            Close(path);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
}
