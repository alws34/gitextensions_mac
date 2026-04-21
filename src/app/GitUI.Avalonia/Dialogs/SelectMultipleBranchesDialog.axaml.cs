using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class SelectMultipleBranchesDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private List<string> _branches = [];

    public IReadOnlyList<string> SelectedBranches { get; private set; } = [];

    public SelectMultipleBranchesDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        _ = LoadBranchesAsync();
    }

    private async Task LoadBranchesAsync()
    {
        var branches = await Task.Run(() =>
            _module.GetRefs(RefsFilter.Heads).Select(r => r.Name).ToList());

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _branches = branches;
            BranchesList.ItemsSource = branches;
        });
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        SelectedBranches = BranchesList.SelectedItems?
            .OfType<string>()
            .ToList() ?? [];
        Close(SelectedBranches);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
}
