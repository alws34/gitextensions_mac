using Avalonia.Controls;
using Avalonia.Interactivity;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class EditorDialog : GitExtensionsDialog
{
    private readonly string? _filePath;

    public EditorDialog(string title, string content, string? filePath = null)
    {
        Title = title;
        _filePath = filePath;
        InitializeComponent();
        TextEdit.Text = content;
    }

    public string EditedText => TextEdit.Text ?? string.Empty;

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (_filePath is not null)
        {
            System.IO.File.WriteAllText(_filePath, TextEdit.Text ?? string.Empty);
        }

        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
