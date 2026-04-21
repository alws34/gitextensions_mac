using Avalonia.Controls;
using Avalonia.Interactivity;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class AddToGitIgnoreDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public AddToGitIgnoreDialog(GitModule module, string pattern = "")
    {
        _module = module;
        InitializeComponent();
        PatternBox.Text = pattern;
        TargetCombo.SelectedIndex = 0;
    }

    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        string pattern = PatternBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return;
        }

        string filePath = TargetCombo.SelectedIndex == 0
            ? System.IO.Path.Combine(_module.WorkingDir, ".gitignore")
            : System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".gitignore_global");

        System.IO.File.AppendAllText(filePath, "\n" + pattern);
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
