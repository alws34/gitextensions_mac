using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class ResetChangesDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public ResetChangesDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        ModeCombo.SelectedIndex = 1;
        ModeCombo.SelectionChanged += ModeCombo_SelectionChanged;
        _ = LoadStatusAsync();
    }

    private async Task LoadStatusAsync()
    {
        var (staged, unstaged) = await Task.Run(() =>
        {
            string status = _module.GitExecutable.GetOutput("status --porcelain");
            var lines = status.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            int s = lines.Count(l => l.Length > 1 && l[0] != ' ' && l[0] != '?');
            int u = lines.Count(l => l.Length > 1 && (l[1] != ' ' || l[0] == '?'));
            return (s, u);
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            StagedCountLabel.Text = $"Staged changes: {staged} files";
            UnstagedCountLabel.Text = $"Unstaged changes: {unstaged} files";
        });
    }

    private void ModeCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        WarningLabel.IsVisible = ModeCombo.SelectedIndex == 2;
    }

    private void Reset_Click(object? sender, RoutedEventArgs e)
    {
        string mode = ModeCombo.SelectedIndex switch
        {
            0 => "--soft",
            1 => "--mixed",
            2 => "--hard",
            _ => "--mixed",
        };

        _ = Task.Run(() => _module.GitExecutable.GetOutput($"reset {mode} HEAD"));
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
