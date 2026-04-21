using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class BisectDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public BisectDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
    }

    private void RunBisect(string subCommand)
    {
        _ = RunBisectAsync(subCommand);
    }

    private async Task RunBisectAsync(string subCommand)
    {
        string result = await Task.Run(() => _module.GitExecutable.GetOutput($"bisect {subCommand}"));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            OutputLabel.Text = result.Trim();
            if (result.Contains("is the first bad commit"))
            {
                StateLabel.Text = "Found first bad commit!";
            }
            else if (subCommand == "start")
            {
                StateLabel.Text = "Bisect started — mark commits good or bad.";
            }
            else if (subCommand == "reset")
            {
                StateLabel.Text = "Not started";
            }
        });
    }

    private void Start_Click(object? sender, RoutedEventArgs e) => RunBisect("start");

    private void Good_Click(object? sender, RoutedEventArgs e) => RunBisect("good");

    private void Bad_Click(object? sender, RoutedEventArgs e) => RunBisect("bad");

    private void Reset_Click(object? sender, RoutedEventArgs e) => RunBisect("reset");

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
}
