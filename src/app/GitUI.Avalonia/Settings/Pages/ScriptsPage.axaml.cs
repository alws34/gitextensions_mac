using Avalonia.Controls;
using GitUI.Avalonia.Settings;

namespace GitUI.Avalonia.Settings.Pages;

public partial class ScriptsPage : UserControl, ISettingsPage
{
    public ScriptsPage()
    {
        InitializeComponent();
        OwnScriptsTextBox.Text = App.Settings.GetString("ownScripts", string.Empty);
    }

    public void SaveSettings()
        => App.Settings.SetString("ownScripts", OwnScriptsTextBox.Text ?? string.Empty);
}
