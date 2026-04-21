using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class CreateBranchDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public CreateBranchDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var branches = await Task.Run(() => _module.GetRefs(RefsFilter.Heads | RefsFilter.Remotes));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            BaseBranchComboBox.ItemsSource = branches.Select(b => b.Name).ToList();
            if (BaseBranchComboBox.Items.Count > 0)
            {
                BaseBranchComboBox.SelectedIndex = 0;
            }
        });
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        string name = BranchNameTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        string baseBranch = BaseBranchComboBox.SelectedItem?.ToString() ?? string.Empty;
        bool checkout = CheckoutAfterCreateCheckBox.IsChecked == true;

        if (checkout)
        {
            _ = Task.Run(() => _module.GitExecutable.GetOutput($"checkout -b {name} {baseBranch}"));
        }
        else
        {
            _ = Task.Run(() => _module.GitExecutable.GetOutput($"branch {name} {baseBranch}"));
        }

        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
