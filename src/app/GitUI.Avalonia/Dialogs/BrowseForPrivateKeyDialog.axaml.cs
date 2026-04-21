using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class BrowseForPrivateKeyDialog : GitExtensionsDialog
{
    public string? SelectedKeyPath { get; private set; }

    public BrowseForPrivateKeyDialog()
    {
        InitializeComponent();
        string sshDir = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
            ".ssh");
        KeyPathBox.Text = sshDir;
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

        string sshDir = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
            ".ssh");

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select SSH Private Key",
            AllowMultiple = false,
            SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(sshDir),
        });

        if (files.Count > 0)
        {
            KeyPathBox.Text = files[0].Path.LocalPath;
        }
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        SelectedKeyPath = KeyPathBox.Text;
        Close(SelectedKeyPath);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
}
