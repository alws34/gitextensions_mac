using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;

namespace GitExtensions.Plugins.ReleaseNotesGenerator;

public partial class ReleaseNotesGeneratorAvaloniaForm : Window
{
    private readonly GitModule _module;

    public ReleaseNotesGeneratorAvaloniaForm(GitModule module)
    {
        _module = module;
        InitializeComponent();
    }

    private void Generate_Click(object? sender, RoutedEventArgs e)
    {
        _ = GenerateAsync();
    }

    private async Task GenerateAsync()
    {
        string from = FromBox.Text ?? string.Empty;
        string to = ToBox.Text ?? "HEAD";
        string range = string.IsNullOrEmpty(from) ? to : $"{from}..{to}";

        string notes = await Task.Run(() =>
        {
            string output = _module.GitExecutable.GetOutput($"log {range} --oneline --no-merges");
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            return string.Join('\n', lines.Select(l =>
            {
                int space = l.IndexOf(' ');
                return space > 0 ? $"- {l[(space + 1)..]}" : $"- {l}";
            }));
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            NotesEditor.Text = notes;
        });
    }

    private void Copy_Click(object? sender, RoutedEventArgs e)
    {
        _ = Clipboard?.SetTextAsync(NotesEditor.Text ?? string.Empty);
    }
}
