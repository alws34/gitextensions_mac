using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class CreateWorktreeDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public CreateWorktreeDialog(GitModule module)
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
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select worktree path",
        });
        if (folders.Count > 0)
        {
            await Dispatcher.UIThread.InvokeAsync(() => PathTextBox.Text = folders[0].Path.LocalPath);
        }
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        _ = CreateAsync();
    }

    private async Task CreateAsync()
    {
        string path = PathTextBox.Text?.Trim() ?? string.Empty;
        string branch = BranchTextBox.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(path))
        {
            await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = "Please enter a path.");
            return;
        }

        string args = NewBranchCheckBox.IsChecked == true
            ? $"worktree add -b {branch} \"{path}\""
            : $"worktree add \"{path}\" {branch}";

        string result = await Task.Run(() => _module.GitExecutable.GetOutput(args));
        await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = result.Trim());

        if (!result.Contains("error") && !result.Contains("fatal"))
        {
            Close(true);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
