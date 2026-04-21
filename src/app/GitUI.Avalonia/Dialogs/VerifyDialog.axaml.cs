using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class VerifyDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public VerifyDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
    }

    private void Run_Click(object? sender, RoutedEventArgs e)
    {
        _ = RunFsckAsync();
    }

    private async Task RunFsckAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => OutputLabel.Text = "Running git fsck...");
        string result = await Task.Run(() => _module.GitExecutable.GetOutput("fsck"));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            OutputLabel.Text = string.IsNullOrWhiteSpace(result) ? "Repository is OK." : result.Trim();
        });
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
}
