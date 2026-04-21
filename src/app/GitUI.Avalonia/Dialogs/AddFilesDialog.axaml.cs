using Avalonia.Controls;
using Avalonia.Interactivity;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class AddFilesDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public AddFilesDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
    }

    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        string pattern = PatternBox.Text ?? "*";
        string force = ForceCheck.IsChecked == true ? " -f" : string.Empty;
        _ = Task.Run(() => _module.GitExecutable.GetOutput($"add{force} {pattern}"));
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
