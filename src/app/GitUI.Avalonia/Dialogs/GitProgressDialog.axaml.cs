using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class GitProgressDialog : GitExtensionsDialog
{
    private readonly Func<System.Threading.Tasks.Task<string>> _operation;

    public GitProgressDialog(string title, Func<System.Threading.Tasks.Task<string>> operation)
    {
        _operation = operation;
        InitializeComponent();
        Title = title;
        _ = RunAsync();
    }

    private async System.Threading.Tasks.Task RunAsync()
    {
        try
        {
            string output = await _operation();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                OutputBox.Text = string.IsNullOrWhiteSpace(output) ? "Done." : output.Trim();
                StatusLabel.Text = "✓ Completed";
                CloseButton.IsEnabled = true;
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                OutputBox.Text = ex.Message;
                StatusLabel.Text = "✗ Failed";
                CloseButton.IsEnabled = true;
            });
        }
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close(null);
}
