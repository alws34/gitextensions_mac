using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class ResolveConflictsDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private List<ConflictItem> _conflicts = [];

    public ResolveConflictsDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        _ = LoadConflictsAsync();
    }

    private async Task LoadConflictsAsync()
    {
        _conflicts = await Task.Run(() =>
        {
            string output = _module.GitExecutable.GetOutput("diff --name-only --diff-filter=U");
            return output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                         .Select(f => new ConflictItem(f, "Both modified"))
                         .ToList();
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ConflictsGrid.ItemsSource = _conflicts.Select(c => c.FileName).ToList();
        });
    }

    private ConflictItem? GetSelected()
    {
        int idx = ConflictsGrid.SelectedIndex;
        return idx >= 0 && idx < _conflicts.Count ? _conflicts[idx] : null;
    }

    private void MergeTool_Click(object? sender, RoutedEventArgs e)
    {
        ConflictItem? item = GetSelected();
        if (item is null)
        {
            return;
        }

        _ = Task.Run(() => _module.GitExecutable.GetOutput($"mergetool \"{item.FileName}\""));
    }

    private void UseOurs_Click(object? sender, RoutedEventArgs e)
    {
        _ = CheckoutAsync("--ours");
    }

    private void UseTheirs_Click(object? sender, RoutedEventArgs e)
    {
        _ = CheckoutAsync("--theirs");
    }

    private async Task CheckoutAsync(string flag)
    {
        ConflictItem? item = GetSelected();
        if (item is null)
        {
            return;
        }

        await Task.Run(() => _module.GitExecutable.GetOutput($"checkout {flag} -- \"{item.FileName}\""));
        await LoadConflictsAsync();
    }

    private void MarkResolved_Click(object? sender, RoutedEventArgs e)
    {
        _ = MarkResolvedAsync();
    }

    private async Task MarkResolvedAsync()
    {
        ConflictItem? item = GetSelected();
        if (item is null)
        {
            return;
        }

        await Task.Run(() => _module.GitExecutable.GetOutput($"add -- \"{item.FileName}\""));
        await LoadConflictsAsync();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private sealed record ConflictItem(string FileName, string ConflictType);
}
