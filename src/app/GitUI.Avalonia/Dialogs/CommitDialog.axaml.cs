using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Base;
using GitUI.Avalonia.CommitDetails;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.Dialogs;

public partial class CommitDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public CommitDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        DiffPreview.TextArea.TextView.LineTransformers.Add(new DiffLineColorizer());
        _ = LoadStatusAsync();
    }

    // -------------------------------------------------------------------------
    // File status loading
    // -------------------------------------------------------------------------

    private async System.Threading.Tasks.Task LoadStatusAsync()
    {
        (IReadOnlyList<GitItemStatus> unstaged, IReadOnlyList<GitItemStatus> staged) =
            await System.Threading.Tasks.Task.Run(() =>
            {
                IReadOnlyList<GitItemStatus> all = _module.GetAllChangedFiles();
                List<GitItemStatus> unstagedItems = [.. all.Where(f => f.Staged == StagedStatus.WorkTree)];
                List<GitItemStatus> stagedItems = [.. all.Where(f => f.Staged == StagedStatus.Index)];
                return (unstagedItems as IReadOnlyList<GitItemStatus>, stagedItems as IReadOnlyList<GitItemStatus>);
            });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            UnstagedList.ItemsSource = unstaged.Select(f => f.Name).ToList();
            StagedList.ItemsSource = staged.Select(f => f.Name).ToList();
        });
    }

    public event Action? CommitMade;

    // -------------------------------------------------------------------------
    // Per-file stage / unstage (double-tap + context menu)
    // -------------------------------------------------------------------------

    private void UnstagedFile_DoubleTapped(object? sender, global::Avalonia.Input.TappedEventArgs e)
    {
        if (UnstagedList.SelectedItem is string fileName)
        {
            _ = StageFileAsync(fileName);
        }
    }

    private void StagedFile_DoubleTapped(object? sender, global::Avalonia.Input.TappedEventArgs e)
    {
        if (StagedList.SelectedItem is string fileName)
        {
            _ = UnstageFileAsync(fileName);
        }
    }

    private void CtxStage_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (UnstagedList.SelectedItem is string f)
        {
            _ = StageFileAsync(f);
        }
    }

    private void CtxUnstage_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (StagedList.SelectedItem is string f)
        {
            _ = UnstageFileAsync(f);
        }
    }

    private void CtxDiffUnstaged_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (UnstagedList.SelectedItem is string f)
        {
            _ = ShowDiffAsync(f, staged: false);
        }
    }

    private void CtxDiffStaged_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (StagedList.SelectedItem is string f)
        {
            _ = ShowDiffAsync(f, staged: true);
        }
    }

    private async System.Threading.Tasks.Task StageFileAsync(string fileName)
    {
        await System.Threading.Tasks.Task.Run(() =>
            _module.GitExecutable.GetOutput($"add -- \"{fileName}\""));
        await LoadStatusAsync();
        await Dispatcher.UIThread.InvokeAsync(() => DiffPreview.Text = string.Empty);
    }

    private async System.Threading.Tasks.Task UnstageFileAsync(string fileName)
    {
        await System.Threading.Tasks.Task.Run(() =>
            _module.GitExecutable.GetOutput($"reset HEAD -- \"{fileName}\""));
        await LoadStatusAsync();
        await Dispatcher.UIThread.InvokeAsync(() => DiffPreview.Text = string.Empty);
    }

    // -------------------------------------------------------------------------
    // Stage / Unstage all
    // -------------------------------------------------------------------------

    private void StageAll_Click(object? sender, RoutedEventArgs e)
    {
        _ = StageAllAsync();
    }

    private async System.Threading.Tasks.Task StageAllAsync()
    {
        await System.Threading.Tasks.Task.Run(() =>
            _module.GitExecutable.GetOutput("add -A"));

        await LoadStatusAsync();
        await Dispatcher.UIThread.InvokeAsync(() => DiffPreview.Text = string.Empty);
    }

    private void UnstageAll_Click(object? sender, RoutedEventArgs e)
    {
        _ = UnstageAllAsync();
    }

    private async System.Threading.Tasks.Task UnstageAllAsync()
    {
        await System.Threading.Tasks.Task.Run(() =>
            _module.GitExecutable.GetOutput("reset HEAD"));

        await LoadStatusAsync();
        await Dispatcher.UIThread.InvokeAsync(() => DiffPreview.Text = string.Empty);
    }

    // -------------------------------------------------------------------------
    // Diff preview on selection
    // -------------------------------------------------------------------------

    private void UnstagedFile_Selected(object? sender, SelectionChangedEventArgs e)
    {
        if (UnstagedList.SelectedItem is string fileName)
        {
            _ = ShowDiffAsync(fileName, staged: false);
        }
    }

    private void StagedFile_Selected(object? sender, SelectionChangedEventArgs e)
    {
        if (StagedList.SelectedItem is string fileName)
        {
            _ = ShowDiffAsync(fileName, staged: true);
        }
    }

    private async System.Threading.Tasks.Task ShowDiffAsync(string fileName, bool staged)
    {
        string diff = await System.Threading.Tasks.Task.Run(() =>
        {
            string args = staged
                ? $"diff --cached -- \"{fileName}\""
                : $"diff -- \"{fileName}\"";
            return _module.GitExecutable.GetOutput(args);
        });

        await Dispatcher.UIThread.InvokeAsync(() => DiffPreview.Text = diff);
    }

    // -------------------------------------------------------------------------
    // Commit
    // -------------------------------------------------------------------------

    private void Commit_Click(object? sender, RoutedEventArgs e)
    {
        _ = CommitAsync();
    }

    private async System.Threading.Tasks.Task CommitAsync()
    {
        string message = CommitMessageEditor.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(message))
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
                DiffPreview.Text = "Error: Please enter a commit message.");
            return;
        }

        bool amend = AmendCheckBox.IsChecked == true;

        string result = await System.Threading.Tasks.Task.Run(() =>
        {
            // Write the message to a temp file to avoid shell quoting issues
            string tmpFile = System.IO.Path.GetTempFileName();
            try
            {
                System.IO.File.WriteAllText(tmpFile, message, System.Text.Encoding.UTF8);
                string amendFlag = amend ? "--amend " : string.Empty;
                string gitArgs = $"commit {amendFlag}-F \"{tmpFile}\"";
                return _module.GitExecutable.GetOutput(gitArgs);
            }
            finally
            {
                try
                {
                    System.IO.File.Delete(tmpFile);
                }
                catch
                {
                    // best-effort cleanup
                }
            }
        });

        bool success = result.Contains("master") || result.Contains("main") ||
                       result.Contains("HEAD") || result.Contains("[");

        if (success)
        {
            CommitMade?.Invoke();
            await Dispatcher.UIThread.InvokeAsync(() => Close(true));
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() => DiffPreview.Text = result);
        }
    }

    // -------------------------------------------------------------------------
    // Amend
    // -------------------------------------------------------------------------

    private void AmendCheckBox_CheckedChanged(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (AmendCheckBox.IsChecked == true)
        {
            _ = LoadAmendMessageAsync();
        }
    }

    private async System.Threading.Tasks.Task LoadAmendMessageAsync()
    {
        string message = await System.Threading.Tasks.Task.Run(
            () => _module.GitExecutable.GetOutput("log -1 --format=%B").Trim());
        await Dispatcher.UIThread.InvokeAsync(
            () => CommitMessageEditor.Text = message);
    }

    // -------------------------------------------------------------------------
    // Cancel
    // -------------------------------------------------------------------------

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
}
