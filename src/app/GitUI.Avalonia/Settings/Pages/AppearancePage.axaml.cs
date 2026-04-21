using Avalonia.Controls;
using GitUI.Avalonia.Settings;

namespace GitUI.Avalonia.Settings.Pages;

public partial class AppearancePage : UserControl, ISettingsPage
{
    public AppearancePage()
    {
        InitializeComponent();
        EditorFontTextBox.Text = App.Settings.GetString("editorFont", "Menlo");
        EditorFontSizeBox.Value = App.Settings.GetInt("editorFontSize", 12);
    }

    public void SaveSettings()
    {
        App.Settings.SetString("editorFont", EditorFontTextBox.Text ?? "Menlo");
        App.Settings.SetInt("editorFontSize", (int)(EditorFontSizeBox.Value ?? 12));
    }
}
