using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtUtils;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class CreateBranchDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private readonly string? _defaultRef;

    public CreateBranchDialog(GitModule module, string? defaultRef = null)
    {
        _module = module;
        _defaultRef = defaultRef;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var refs = await Task.Run(() => _module.GetRefs(RefsFilter.Heads | RefsFilter.Remotes | RefsFilter.Tags));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var candidates = refs
                .Select(b => b.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!string.IsNullOrWhiteSpace(_defaultRef)
                && !candidates.Contains(_defaultRef, StringComparer.OrdinalIgnoreCase))
            {
                candidates.Insert(0, _defaultRef);
            }

            BaseBranchComboBox.ItemsSource = candidates;
            int defaultIndex = string.IsNullOrWhiteSpace(_defaultRef)
                ? -1
                : candidates.FindIndex(name => string.Equals(name, _defaultRef, StringComparison.OrdinalIgnoreCase));
            BaseBranchComboBox.SelectedIndex = defaultIndex >= 0
                ? defaultIndex
                : BaseBranchComboBox.Items.Count > 0 ? 0 : -1;
        });
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        string name = BranchNameTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        string baseBranch = BaseBranchComboBox.SelectedItem?.ToString() ?? "HEAD";
        bool checkout = CheckoutAfterCreateCheckBox.IsChecked == true;

        if (checkout)
        {
            _ = Task.Run(() => _module.GitExecutable.GetOutput($"checkout -b {name.Quote()} {baseBranch.Quote()}"));
        }
        else
        {
            _ = Task.Run(() => _module.GitExecutable.GetOutput($"branch {name.Quote()} {baseBranch.Quote()}"));
        }

        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
