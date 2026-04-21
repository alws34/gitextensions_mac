using Avalonia.Controls;
using GitUI.Avalonia.Settings;

namespace GitUI.Avalonia.Settings.Pages;

public partial class AdvancedPage : UserControl, ISettingsPage
{
    public AdvancedPage()
    {
        InitializeComponent();
        VerboseCheckBox.IsChecked = App.Settings.GetBool("verboseOutput", false);
        ShowBuildServerCheckBox.IsChecked = App.Settings.GetBool("showBuildServer", false);
    }

    public void SaveSettings()
    {
        App.Settings.SetBool("verboseOutput", VerboseCheckBox.IsChecked == true);
        App.Settings.SetBool("showBuildServer", ShowBuildServerCheckBox.IsChecked == true);
    }
}
