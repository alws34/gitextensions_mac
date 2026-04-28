using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GitUI.Avalonia.Settings;

namespace GitUI.Avalonia.Settings.Pages;

public partial class SshPage : UserControl, ISettingsPage
{
    public SshPage()
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        string client = App.Settings.GetString("sshClient", "openssh");
        SshClientComboBox.SelectedIndex = client == "putty" ? 1 : 0;

        SshKeyPathTextBox.Text = App.Settings.GetString("sshKeyPath", string.Empty);
        UseSshAgentCheckBox.IsChecked = App.Settings.GetBool("useSshAgent", false);
    }

    public void SaveSettings()
    {
        string client = SshClientComboBox.SelectedIndex == 1 ? "putty" : "openssh";
        App.Settings.SetString("sshClient", client);
        App.Settings.SetString("sshKeyPath", SshKeyPathTextBox.Text?.Trim() ?? string.Empty);
        App.Settings.SetBool("useSshAgent", UseSshAgentCheckBox.IsChecked == true);
    }

    private void BrowseKey_Click(object? sender, RoutedEventArgs e)
    {
        _ = BrowseKeyAsync();
    }

    private async Task BrowseKeyAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select SSH key file",
        });
        if (files.Count > 0)
        {
            SshKeyPathTextBox.Text = files[0].Path.LocalPath;
        }
    }
}
