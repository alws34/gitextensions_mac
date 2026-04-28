using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GitUI.Avalonia.Settings;

namespace GitUI.Avalonia.Settings.Pages;

public partial class DiffToolsPage : UserControl, ISettingsPage
{
    private static readonly string[] ToolTags = ["none", "vscode", "meld", "filemerge", "kaleidoscope", "bbedit", "custom"];

    public DiffToolsPage()
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        string diffTool = App.Settings.GetString("diffTool", "none");
        string mergeTool = App.Settings.GetString("mergeTool", "none");

        DiffToolComboBox.SelectedIndex = IndexOfTag(diffTool);
        MergeToolComboBox.SelectedIndex = IndexOfTag(mergeTool);

        CustomDiffCommandTextBox.Text = App.Settings.GetString("customDiffCommand", string.Empty);
        UpdateCustomCommandVisibility();
    }

    public void SaveSettings()
    {
        App.Settings.SetString("diffTool", TagAtIndex(DiffToolComboBox.SelectedIndex));
        App.Settings.SetString("mergeTool", TagAtIndex(MergeToolComboBox.SelectedIndex));
        App.Settings.SetString("customDiffCommand", CustomDiffCommandTextBox.Text?.Trim() ?? string.Empty);
    }

    private void DiffTool_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateCustomCommandVisibility();
    }

    private void UpdateCustomCommandVisibility()
    {
        CustomCommandRow.IsVisible = TagAtIndex(DiffToolComboBox.SelectedIndex) == "custom";
    }

    private void BrowseCustomCommand_Click(object? sender, RoutedEventArgs e)
    {
        _ = BrowseCustomCommandAsync();
    }

    private async Task BrowseCustomCommandAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select diff tool executable",
        });
        if (files.Count > 0)
        {
            CustomDiffCommandTextBox.Text = files[0].Path.LocalPath;
        }
    }

    private static int IndexOfTag(string tag)
    {
        int idx = Array.IndexOf(ToolTags, tag);
        return idx < 0 ? 0 : idx;
    }

    private static string TagAtIndex(int index)
    {
        if (index < 0 || index >= ToolTags.Length)
        {
            return "none";
        }

        return ToolTags[index];
    }
}
