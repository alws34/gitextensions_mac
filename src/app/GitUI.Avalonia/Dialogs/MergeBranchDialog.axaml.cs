using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtUtils;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class MergeBranchDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private readonly string? _defaultRef;

    public MergeBranchDialog(GitModule module, string? defaultRef = null)
    {
        _module = module;
        _defaultRef = defaultRef;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var refs = await Task.Run(() => _module.GetRefs(RefsFilter.Heads | RefsFilter.Remotes | RefsFilter.Tags));
        var refNames = refs
            .Where(gitRef => !gitRef.IsDereference)
            .Select(gitRef => gitRef.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            BranchComboBox.ItemsSource = refNames;
            if (refNames.Count == 0)
            {
                return;
            }

            int defaultIndex = string.IsNullOrWhiteSpace(_defaultRef)
                ? -1
                : refNames.FindIndex(name => string.Equals(name, _defaultRef, StringComparison.OrdinalIgnoreCase));
            BranchComboBox.SelectedIndex = defaultIndex >= 0 ? defaultIndex : 0;
        });
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        _ = MergeAsync();
    }

    private async Task MergeAsync()
    {
        string branch = BranchComboBox.SelectedItem?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(branch))
        {
            return;
        }

        string flag = NoFastForwardRadio.IsChecked == true ? "--no-ff"
            : SquashRadio.IsChecked == true ? "--squash"
            : string.Empty;

        string command = string.IsNullOrEmpty(flag)
            ? $"merge {branch.Quote()}"
            : $"merge {flag} {branch.Quote()}";
        await Task.Run(() => _module.GitExecutable.GetOutput(command));
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
