using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class SubmodulesDialog : GitExtensionsDialog
{
    private readonly GitModule _module;

    public SubmodulesDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        _ = LoadSubmodulesAsync();
    }

    private async Task LoadSubmodulesAsync()
    {
        var paths = await Task.Run(() => _module.GetSubmodulesLocalPaths());
        var items = paths.ToList();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            SubmodulesGrid.ItemsSource = items;
        });
    }

    private void UpdateAll_Click(object? sender, RoutedEventArgs e)
    {
        _ = Task.Run(() => _module.GitExecutable.GetOutput("submodule update --init --recursive"));
    }

    private void Sync_Click(object? sender, RoutedEventArgs e)
    {
        _ = Task.Run(() => _module.GitExecutable.GetOutput("submodule sync"));
    }

    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        _ = AddSubmoduleAsync();
    }

    private async Task AddSubmoduleAsync()
    {
        var parent = TopLevel.GetTopLevel(this) as Window;
        if (parent is null)
        {
            return;
        }

        var dialog = new AddSubmoduleDialog(_module);
        bool? result = await dialog.ShowDialog<bool?>(parent);
        if (result == true)
        {
            await LoadSubmodulesAsync();
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
}
