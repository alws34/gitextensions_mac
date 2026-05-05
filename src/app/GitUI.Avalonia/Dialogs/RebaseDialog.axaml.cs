using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtUtils;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class RebaseDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private readonly string? _defaultOnto;

    public RebaseDialog(GitModule module, string? defaultOnto = null)
    {
        _module = module;
        _defaultOnto = defaultOnto;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var branches = await Task.Run(() => _module.GetRefs(RefsFilter.Heads | RefsFilter.Remotes));
        var branchNames = branches
            .Select(branch => branch.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            BranchComboBox.ItemsSource = branchNames;
            if (branchNames.Count == 0)
            {
                return;
            }

            int defaultIndex = string.IsNullOrWhiteSpace(_defaultOnto)
                ? -1
                : branchNames.FindIndex(name => string.Equals(name, _defaultOnto, StringComparison.OrdinalIgnoreCase));
            BranchComboBox.SelectedIndex = defaultIndex >= 0 ? defaultIndex : 0;
        });
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        _ = RebaseAsync();
    }

    private async Task RebaseAsync()
    {
        string onto = BranchComboBox.SelectedItem?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(onto))
        {
            return;
        }

        string interactive = InteractiveCheckBox.IsChecked == true ? "-i " : string.Empty;
        string autostash = AutostashCheckBox.IsChecked == true ? "--autostash " : string.Empty;
        await Task.Run(() =>
            _module.GitExecutable.GetOutput($"rebase {interactive}{autostash}{onto.Quote()}".TrimEnd()));
        Close(true);
    }

    private void Continue_Click(object? sender, RoutedEventArgs e) => _ = RunRebaseControlAsync("--continue");

    private void Skip_Click(object? sender, RoutedEventArgs e) => _ = RunRebaseControlAsync("--skip");

    private void Abort_Click(object? sender, RoutedEventArgs e) => _ = RunRebaseControlAsync("--abort");

    private async Task RunRebaseControlAsync(string flag)
    {
        await Task.Run(() => _module.GitExecutable.GetOutput($"rebase {flag}"));
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
