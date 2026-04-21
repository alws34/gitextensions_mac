using Avalonia.Controls;
using Avalonia.Interactivity;
using GitUI.Avalonia.Base;
using GitUI.Avalonia.Settings.Pages;

namespace GitUI.Avalonia.Settings;

public partial class SettingsWindow : GitExtensionsWindow
{
    private readonly GitPage _gitPage = new();
    private readonly AppearancePage _appearancePage = new();
    private readonly CommitPage _commitPage = new();
    private readonly AdvancedPage _advancedPage = new();

    public SettingsWindow()
    {
        InitializeComponent();
        PageContent.Content = _gitPage;
    }

    private void Category_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (CategoryList.SelectedItem is not ListBoxItem item)
        {
            return;
        }

        PageContent.Content = item.Tag?.ToString() switch
        {
            "git" => (object)_gitPage,
            "appearance" => _appearancePage,
            "commit" => _commitPage,
            "advanced" => _advancedPage,
            _ => _gitPage,
        };
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        _gitPage.SaveSettings();
        _appearancePage.SaveSettings();
        _commitPage.SaveSettings();
        _advancedPage.SaveSettings();
        App.Settings.Save();
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();
}
