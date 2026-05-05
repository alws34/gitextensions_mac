using Avalonia.Controls;
using Avalonia.Interactivity;
using GitUI.Avalonia.Base;
using GitUI.Avalonia.Settings.Pages;

namespace GitUI.Avalonia.Settings;

public partial class SettingsWindow : GitExtensionsWindow
{
    private readonly GeneralPage _generalPage = new();
    private readonly GitPage _gitPage = new();
    private readonly RevisionGridPage _revisionGridPage = new();
    private readonly DiffViewerPage _diffViewerPage = new();
    private readonly AppearancePage _appearancePage = new();
    private readonly CommitPage _commitPage = new();
    private readonly AdvancedPage _advancedPage = new();
    private readonly SshPage _sshPage = new();
    private readonly DiffToolsPage _diffToolsPage = new();
    private readonly CredentialsPage _credentialsPage = new();
    private readonly ConfirmationsPage _confirmationsPage = new();
    private readonly HotkeysPage _hotkeysPage = new();
    private readonly ScriptsPage _scriptsPage = new();
    private readonly RevisionLinksPage _revisionLinksPage = new();
    private readonly BuildServerPage _buildServerPage = new();
    private readonly PluginsPage _pluginsPage = new();

    public event Action? SettingsSaved;

    public SettingsWindow()
    {
        InitializeComponent();
        PageContent.Content = _generalPage;
    }

    private void Category_Changed(object? sender, SelectionChangedEventArgs e)
    {
        if (CategoryList.SelectedItem is not ListBoxItem item)
        {
            return;
        }

        PageContent.Content = item.Tag?.ToString() switch
        {
            "general" => (object)_generalPage,
            "git" => (object)_gitPage,
            "revisiongrid" => _revisionGridPage,
            "diffviewer" => _diffViewerPage,
            "appearance" => _appearancePage,
            "commit" => _commitPage,
            "advanced" => _advancedPage,
            "ssh" => _sshPage,
            "difftools" => _diffToolsPage,
            "credentials" => _credentialsPage,
            "confirmations" => _confirmationsPage,
            "hotkeys" => _hotkeysPage,
            "scripts" => _scriptsPage,
            "revisionlinks" => _revisionLinksPage,
            "buildserver" => _buildServerPage,
            "plugins" => _pluginsPage,
            _ => _generalPage,
        };
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        _generalPage.SaveSettings();
        _gitPage.SaveSettings();
        _revisionGridPage.SaveSettings();
        _diffViewerPage.SaveSettings();
        _appearancePage.SaveSettings();
        _commitPage.SaveSettings();
        _advancedPage.SaveSettings();
        _sshPage.SaveSettings();
        _diffToolsPage.SaveSettings();
        _credentialsPage.SaveSettings();
        _confirmationsPage.SaveSettings();
        _hotkeysPage.SaveSettings();
        _scriptsPage.SaveSettings();
        _revisionLinksPage.SaveSettings();
        _buildServerPage.SaveSettings();
        _pluginsPage.SaveSettings();
        App.Settings.Save();
        SettingsSaved?.Invoke();
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();
}
