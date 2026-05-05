using Avalonia.Controls;
using GitUI.Avalonia.Settings;

namespace GitUI.Avalonia.Settings.Pages;

public partial class RevisionLinksPage : UserControl, ISettingsPage
{
    public RevisionLinksPage()
    {
        InitializeComponent();
        RevisionLinkDefsTextBox.Text = App.Settings.GetString("RevisionLinkDefs", string.Empty);
    }

    public void SaveSettings()
        => App.Settings.SetString("RevisionLinkDefs", RevisionLinkDefsTextBox.Text ?? string.Empty);
}
