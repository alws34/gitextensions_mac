using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class SparseWorkingCopyDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private readonly List<string> _patterns = [];

    public SparseWorkingCopyDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        string sparseFile = Path.Combine(_module.WorkingDir, ".git", "info", "sparse-checkout");
        bool enabled = await Task.Run(() =>
        {
            string config = _module.GitExecutable.GetOutput("config core.sparseCheckout");
            return config.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        });

        List<string> patterns = [];
        if (File.Exists(sparseFile))
        {
            patterns = (await Task.Run(() => File.ReadAllLines(sparseFile))).ToList();
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            EnableCheck.IsChecked = enabled;
            _patterns.Clear();
            _patterns.AddRange(patterns);
            PatternsList.ItemsSource = new List<string>(_patterns);
        });
    }

    private void EnableCheck_Changed(object? sender, RoutedEventArgs e)
    {
        bool enable = EnableCheck.IsChecked == true;
        _ = Task.Run(() =>
            _module.GitExecutable.GetOutput($"config core.sparseCheckout {(enable ? "true" : "false")}"));
    }

    private void AddPattern_Click(object? sender, RoutedEventArgs e)
    {
        string pattern = PatternBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(pattern))
        {
            return;
        }

        _patterns.Add(pattern);
        PatternsList.ItemsSource = new List<string>(_patterns);
        PatternBox.Text = string.Empty;
    }

    private void RemovePattern_Click(object? sender, RoutedEventArgs e)
    {
        if (PatternsList.SelectedItem is string selected)
        {
            _patterns.Remove(selected);
            PatternsList.ItemsSource = new List<string>(_patterns);
        }
    }

    private void Apply_Click(object? sender, RoutedEventArgs e)
    {
        string sparseFile = Path.Combine(_module.WorkingDir, ".git", "info", "sparse-checkout");
        File.WriteAllLines(sparseFile, _patterns);
        _ = Task.Run(() => _module.GitExecutable.GetOutput("sparse-checkout reapply"));
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close(null);
}
