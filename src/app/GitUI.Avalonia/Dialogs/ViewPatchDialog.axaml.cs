using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GitUI.Avalonia.Base;
using GitUI.Avalonia.CommitDetails;

namespace GitUI.Avalonia.Dialogs;

public partial class ViewPatchDialog : GitExtensionsDialog
{
    private string _patchPath = string.Empty;
    private string _repoPath = string.Empty;

    public ViewPatchDialog()
    {
        InitializeComponent();
        PatchViewer.TextArea.TextView.LineTransformers.Add(new DiffLineColorizer());
    }

    private void Open_Click(object? sender, RoutedEventArgs e)
    {
        _ = OpenAsync();
    }

    private async System.Threading.Tasks.Task OpenAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open patch file",
            FileTypeFilter = [new FilePickerFileType("Patch files") { Patterns = ["*.patch", "*.diff"] }],
        });

        if (files.Count == 0)
        {
            return;
        }

        _patchPath = files[0].Path.LocalPath;
        await Dispatcher.UIThread.InvokeAsync(() => PatchFileTextBox.Text = _patchPath);

        string content = await System.Threading.Tasks.Task.Run(
            () => System.IO.File.ReadAllText(_patchPath));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            PatchViewer.Text = content;
            UpdateApplyButtonState();
        });
    }

    private void BrowseRepo_Click(object? sender, RoutedEventArgs e)
    {
        _ = BrowseRepoAsync();
    }

    private async System.Threading.Tasks.Task BrowseRepoAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select repository working directory",
        });

        if (folders.Count == 0)
        {
            return;
        }

        _repoPath = folders[0].Path.LocalPath;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            RepoPathBox.Text = _repoPath;
            UpdateApplyButtonState();
        });
    }

    private void UpdateApplyButtonState()
    {
        ApplyButton.IsEnabled = !string.IsNullOrEmpty(_patchPath)
            && !string.IsNullOrEmpty(_repoPath);
    }

    private void Apply_Click(object? sender, RoutedEventArgs e)
    {
        _ = ApplyPatchAsync();
    }

    private async System.Threading.Tasks.Task ApplyPatchAsync()
    {
        ApplyButton.IsEnabled = false;
        ApplyStatusLabel.Text = "Applying…";

        try
        {
            string result = await System.Threading.Tasks.Task.Run(() =>
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    WorkingDirectory = _repoPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                psi.ArgumentList.Add("apply");
                psi.ArgumentList.Add("--");
                psi.ArgumentList.Add(_patchPath);

                using var process = System.Diagnostics.Process.Start(psi);
                if (process is null)
                {
                    return "Could not start git process.";
                }

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();
                return process.ExitCode == 0
                    ? (string.IsNullOrWhiteSpace(stdout) ? "Patch applied successfully." : stdout.Trim())
                    : $"git apply failed:\n{stderr.Trim()}";
            });

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                bool success = !result.StartsWith("git apply failed", StringComparison.Ordinal)
                    && !result.StartsWith("Could not start", StringComparison.Ordinal);
                ApplyStatusLabel.Text = success ? "✓ Applied" : "✗ Failed";
                PatchViewer.Text += $"\n\n--- Result ---\n{result}";
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ApplyStatusLabel.Text = "✗ Error";
                PatchViewer.Text += $"\n\n--- Error ---\n{ex.Message}";
            });
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => ApplyButton.IsEnabled = true);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
}
