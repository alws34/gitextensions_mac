using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class CreateTagDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public CreateTagDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        _ = CreateTagAsync();
    }

    private async Task CreateTagAsync()
    {
        string name = TagNameTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(name))
        {
            await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = "Please enter a tag name.");
            return;
        }

        string commit = CommitTextBox.Text?.Trim() ?? string.Empty;
        string commitArg = string.IsNullOrEmpty(commit) ? "HEAD" : commit;

        string result;
        if (LightweightRadio.IsChecked == true)
        {
            result = await Task.Run(() => _module.GitExecutable.GetOutput($"tag {name} {commitArg}"));
        }
        else
        {
            string message = MessageTextBox.Text?.Trim() ?? string.Empty;
            string tmpFile = System.IO.Path.GetTempFileName();
            try
            {
                System.IO.File.WriteAllText(tmpFile, message);
                result = await Task.Run(() => _module.GitExecutable.GetOutput($"tag -a {name} {commitArg} -F \"{tmpFile}\""));
            }
            finally
            {
                try
                {
                    System.IO.File.Delete(tmpFile);
                }
                catch
                {
                    // best-effort
                }
            }
        }

        await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = result.Trim());
        if (string.IsNullOrWhiteSpace(result) || !result.Contains("error"))
        {
            Close(true);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
