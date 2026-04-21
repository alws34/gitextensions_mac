using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class GitIgnoreDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private readonly string _filePath;

    public GitIgnoreDialog(GitModule module)
    {
        _module = module;
        _filePath = Path.Combine(_module.WorkingDir, ".gitignore");
        InitializeComponent();
        LoadFile();
    }

    private void LoadFile()
    {
        Editor.Text = File.Exists(_filePath) ? File.ReadAllText(_filePath) : string.Empty;
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        File.WriteAllText(_filePath, Editor.Text ?? string.Empty);
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
