using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class RebaseDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public RebaseDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var branches = await Task.Run(() => _module.GetRefs(RefsFilter.Heads));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            BranchComboBox.ItemsSource = branches.Select(b => b.Name).ToList();
            if (BranchComboBox.Items.Count > 0)
            {
                BranchComboBox.SelectedIndex = 0;
            }
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
            _module.GitExecutable.GetOutput($"rebase {interactive}{autostash}{onto}".TrimEnd()));
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
