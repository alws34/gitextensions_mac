using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class ApplyPatchDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public ApplyPatchDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
    }

    private void Browse_Click(object? sender, RoutedEventArgs e)
    {
        _ = BrowseAsync();
    }

    private async Task BrowseAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select patch file",
            FileTypeFilter = [new FilePickerFileType("Patch files") { Patterns = ["*.patch", "*.diff"] }],
        });
        if (files.Count > 0)
        {
            await Dispatcher.UIThread.InvokeAsync(() => PatchFileTextBox.Text = files[0].Path.LocalPath);
        }
    }

    private void Apply_Click(object? sender, RoutedEventArgs e)
    {
        _ = ApplyAsync();
    }

    private async Task ApplyAsync()
    {
        string patchFile = PatchFileTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(patchFile))
        {
            await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = "Please select a patch file.");
            return;
        }

        string whitespaceFlag = WhitespaceCombo.SelectedIndex switch
        {
            1 => "--whitespace=warn",
            2 => "--whitespace=fix",
            3 => "--whitespace=strip",
            _ => string.Empty,
        };

        string args = string.IsNullOrEmpty(whitespaceFlag)
            ? $"apply \"{patchFile}\""
            : $"apply {whitespaceFlag} \"{patchFile}\"";

        string result = await Task.Run(() => _module.GitExecutable.GetOutput(args));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusLabel.Text = string.IsNullOrWhiteSpace(result) ? "Patch applied successfully." : result.Trim();
        });

        if (!result.Contains("error") && !result.Contains("does not apply"))
        {
            Close(true);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
