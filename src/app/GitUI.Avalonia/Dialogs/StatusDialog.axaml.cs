using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class StatusDialog : GitExtensionsDialog
{
    public StatusDialog(string title, Func<IProgress<string>, Task> operation)
    {
        Title = title;
        InitializeComponent();
        _ = RunAsync(operation);
    }

    private async Task RunAsync(Func<IProgress<string>, Task> operation)
    {
        var progress = new Progress<string>(line =>
        {
            _ = Dispatcher.UIThread.InvokeAsync(() =>
            {
                OutputLog.Text += line + "\n";
            });
        });

        try
        {
            await operation(progress);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = 100;
                CloseButton.IsEnabled = true;
                StatusLabel.Text = "Done.";
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProgressBar.IsIndeterminate = false;
                CloseButton.IsEnabled = true;
                StatusLabel.Text = $"Error: {ex.Message}";
            });
        }
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close(null);
}
