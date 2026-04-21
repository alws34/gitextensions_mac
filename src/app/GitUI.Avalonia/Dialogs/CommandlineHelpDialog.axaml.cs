using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class CommandlineHelpDialog : GitExtensionsDialog
{
    public CommandlineHelpDialog()
    {
        InitializeComponent();
        CommandBox.Text = "commit";
    }

    private void Help_Click(object? sender, RoutedEventArgs e)
    {
        _ = LoadHelpAsync();
    }

    private async Task LoadHelpAsync()
    {
        string command = CommandBox.Text?.Trim() ?? string.Empty;
        string output = await Task.Run(() =>
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("git", $"{command} --help")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                using var proc = System.Diagnostics.Process.Start(psi);
                string result = proc?.StandardOutput.ReadToEnd() ?? string.Empty;
                proc?.WaitForExit();
                return result;
            }
            catch
            {
                return $"Could not get help for: {command}";
            }
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            HelpEditor.Text = output;
        });
    }
}
