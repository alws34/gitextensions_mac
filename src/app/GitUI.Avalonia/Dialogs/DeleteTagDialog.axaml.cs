using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitExtUtils;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class DeleteTagDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private readonly string? _defaultTag;

    public DeleteTagDialog(GitModule module, string? defaultTag = null)
    {
        _module = module;
        _defaultTag = defaultTag;
        InitializeComponent();
        _ = LoadTagsAsync();
    }

    private async Task LoadTagsAsync()
    {
        string output = await Task.Run(() => _module.GitExecutable.GetOutput("tag"));
        var tags = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            TagsList.ItemsSource = tags;
            string? selectedTag = string.IsNullOrWhiteSpace(_defaultTag)
                ? null
                : tags.FirstOrDefault(tag => string.Equals(tag, _defaultTag, StringComparison.OrdinalIgnoreCase));
            if (selectedTag is not null)
            {
                TagsList.SelectedItems?.Add(selectedTag);
            }
        });
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        _ = DeleteAsync();
    }

    private async Task DeleteAsync()
    {
        var selected = TagsList.SelectedItems?.Cast<string>().ToList() ?? [];
        if (selected.Count == 0)
        {
            return;
        }

        await Task.Run(() =>
        {
            foreach (string tag in selected)
            {
                _module.GitExecutable.GetOutput($"tag -d {tag.Quote()}");
            }
        });

        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
