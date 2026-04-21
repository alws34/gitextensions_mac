using Avalonia.Controls;
using GitUI.Avalonia.Settings;

namespace GitUI.Avalonia.Settings.Pages;

public partial class CommitPage : UserControl, ISettingsPage
{
    public CommitPage()
    {
        InitializeComponent();
        TemplateTextBox.Text = App.Settings.GetString("commitTemplate", string.Empty);
        AutoStageCheckBox.IsChecked = App.Settings.GetBool("autoStage", false);
    }

    public void SaveSettings()
    {
        App.Settings.SetString("commitTemplate", TemplateTextBox.Text ?? string.Empty);
        App.Settings.SetBool("autoStage", AutoStageCheckBox.IsChecked == true);
    }
}
