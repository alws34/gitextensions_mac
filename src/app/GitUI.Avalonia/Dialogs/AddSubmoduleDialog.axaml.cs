using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class AddSubmoduleDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public AddSubmoduleDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        _ = AddAsync();
    }

    private async Task AddAsync()
    {
        string url = UrlTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(url))
        {
            await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = "Please enter a repository URL.");
            return;
        }

        string path = PathTextBox.Text?.Trim() ?? string.Empty;
        string branch = BranchTextBox.Text?.Trim() ?? string.Empty;

        string args = "submodule add";
        if (!string.IsNullOrEmpty(branch))
        {
            args += $" -b {branch}";
        }

        args += $" {url}";
        if (!string.IsNullOrEmpty(path))
        {
            args += $" {path}";
        }

        string result = await Task.Run(() => _module.GitExecutable.GetOutput(args));
        await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = result.Trim());

        if (!result.Contains("error") && !result.Contains("fatal"))
        {
            Close(true);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
