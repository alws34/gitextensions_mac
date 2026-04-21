using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class CheckoutBranchDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public CheckoutBranchDialog(GitModule module)
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

    private void CreateNewBranch_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        NewBranchPanel.IsVisible = CreateNewBranchCheckBox.IsChecked == true;
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        if (CreateNewBranchCheckBox.IsChecked == true)
        {
            string newBranchName = NewBranchNameTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(newBranchName))
            {
                return;
            }

            string baseBranch = BranchComboBox.SelectedItem?.ToString() ?? string.Empty;
            _ = Task.Run(() => _module.GitExecutable.GetOutput($"checkout -b {newBranchName} {baseBranch}"));
        }
        else
        {
            string selectedBranch = BranchComboBox.SelectedItem?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(selectedBranch))
            {
                return;
            }

            _ = Task.Run(() => _module.GitExecutable.GetOutput($"checkout {selectedBranch}"));
        }

        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
