using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class LogDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public LogDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        _ = LoadLogAsync();
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        _ = LoadLogAsync();
    }

    private async Task LoadLogAsync()
    {
        bool oneline = OnelineCheck.IsChecked == true;
        bool graph = GraphCheck.IsChecked == true;
        bool all = AllCheck.IsChecked == true;

        string args = "log";
        if (oneline)
        {
            args += " --oneline";
        }

        if (graph)
        {
            args += " --graph";
        }

        if (all)
        {
            args += " --all";
        }

        args += " --max-count=500";

        string output = await Task.Run(() => _module.GitExecutable.GetOutput(args));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            LogEditor.Text = output;
        });
    }
}
