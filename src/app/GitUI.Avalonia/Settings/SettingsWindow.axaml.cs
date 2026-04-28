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
    private readonly SshPage _sshPage = new();
    private readonly DiffToolsPage _diffToolsPage = new();
    private readonly CredentialsPage _credentialsPage = new();

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
            "ssh" => _sshPage,
            "difftools" => _diffToolsPage,
            "credentials" => _credentialsPage,
            _ => _gitPage,
        };
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        _gitPage.SaveSettings();
        _appearancePage.SaveSettings();
        _commitPage.SaveSettings();
        _advancedPage.SaveSettings();
        _sshPage.SaveSettings();
        _diffToolsPage.SaveSettings();
        _credentialsPage.SaveSettings();
        App.Settings.Save();
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();
}
