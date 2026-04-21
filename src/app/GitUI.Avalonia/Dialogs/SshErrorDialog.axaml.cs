using Avalonia.Interactivity;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class SshErrorDialog : GitExtensionsDialog
{
    public SshErrorDialog(string errorMessage = "")
    {
        InitializeComponent();
        ErrorMessageLabel.Text = errorMessage;
    }

    private void StartAgent_Click(object? sender, RoutedEventArgs e)
    {
        _ = Task.Run(() =>
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("ssh-agent")
                {
                    UseShellExecute = false,
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch
            {
                // ssh-agent not available
            }
        });
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close(null);
}
