using System.IO;
using Avalonia.Interactivity;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class MailMapDialog : GitExtensionsDialog
{
    private readonly string _filePath;

    public MailMapDialog(GitModule module)
    {
        _filePath = Path.Combine(module.WorkingDir, ".mailmap");
        InitializeComponent();
        MailMapEditor.Text = File.Exists(_filePath)
            ? File.ReadAllText(_filePath)
            : "# .mailmap — map author names and emails\n# Format: Proper Name <proper@email.com> Commit Name <commit@email.com>\n";
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        File.WriteAllText(_filePath, MailMapEditor.Text ?? string.Empty);
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
