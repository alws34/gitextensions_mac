using Avalonia.Controls;
using GitUI.Avalonia.Settings;

namespace GitUI.Avalonia.Settings.Pages;

public partial class GeneralPage : UserControl, ISettingsPage
{
    public GeneralPage()
    {
        InitializeComponent();
        OpenLastRepositoryCheckBox.IsChecked = App.Settings.GetBool("openLastRepositoryOnStartup", true);
        RecentRepositoryLimitBox.Value = App.Settings.GetInt("recentRepositoriesLimit", 20);
        UseNativeMenuCheckBox.IsChecked = App.Settings.GetBool("useNativeMenu", true);
    }

    public void SaveSettings()
    {
        App.Settings.SetBool("openLastRepositoryOnStartup", OpenLastRepositoryCheckBox.IsChecked == true);
        App.Settings.SetInt("recentRepositoriesLimit", (int)(RecentRepositoryLimitBox.Value ?? 20));
        App.Settings.SetBool("useNativeMenu", UseNativeMenuCheckBox.IsChecked == true);
    }
}
