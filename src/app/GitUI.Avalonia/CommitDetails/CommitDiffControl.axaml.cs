using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.CommitDetails;

public partial class CommitDiffControl : GitModuleControl
{
    public CommitDiffControl()
    {
        InitializeComponent();
        DiffEditor.TextArea.TextView.LineTransformers.Add(new DiffLineColorizer());
    }

    public async System.Threading.Tasks.Task ShowDiffAsync(GitRevision revision, string? filePath = null)
    {
        if (Module is null)
        {
            return;
        }

        try
        {
            string gitArgs = filePath is null
                ? $"diff-tree --no-commit-id -p {revision.Guid}"
                : $"diff-tree --no-commit-id -p {revision.Guid} -- \"{filePath}\"";

            string diff = await Module.GitExecutable.GetOutputAsync(gitArgs);

            await Dispatcher.UIThread.InvokeAsync(() => DiffEditor.Text = diff);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShowDiffAsync failed: {ex.Message}");
        }
    }
}
