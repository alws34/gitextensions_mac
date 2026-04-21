using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class DiffDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public DiffDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        RefABox.Text = "HEAD~1";
        RefBBox.Text = "HEAD";
    }

    private void Diff_Click(object? sender, RoutedEventArgs e)
    {
        _ = LoadDiffAsync();
    }

    private async Task LoadDiffAsync()
    {
        string refA = RefABox.Text ?? "HEAD~1";
        string refB = RefBBox.Text ?? "HEAD";
        string diff = await Task.Run(() =>
            _module.GitExecutable.GetOutput($"diff {refA} {refB}"));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            DiffEditor.Text = diff;
        });
    }
}
