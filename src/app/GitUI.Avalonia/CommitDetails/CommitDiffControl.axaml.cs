using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.CommitDetails;

public partial class CommitDiffControl : GitModuleControl
{
    public CommitDiffControl() => InitializeComponent();

    public async System.Threading.Tasks.Task ShowDiffAsync(GitRevision revision, string? filePath = null)
    {
        if (Module is null)
        {
            return;
        }

        string diff = await System.Threading.Tasks.Task.Run(() =>
        {
            string gitArgs = filePath is null
                ? $"diff-tree --no-commit-id -p {revision.Guid}"
                : $"diff-tree --no-commit-id -p {revision.Guid} -- \"{filePath}\"";
            return Module.GitExecutable.GetOutput(gitArgs);
        });

        await Dispatcher.UIThread.InvokeAsync(() => DiffEditor.Text = diff);
    }
}
