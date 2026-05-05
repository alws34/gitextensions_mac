using Avalonia.Controls;
using Avalonia.Interactivity;
using GitUI.Avalonia.Settings;

namespace GitUI.Avalonia.Settings.Pages;

public partial class HotkeysPage : UserControl, ISettingsPage
{
    public HotkeysPage()
    {
        InitializeComponent();
        SerializedHotkeysTextBox.Text = App.Settings.GetString("SerializedHotkeys", string.Empty);
    }

    public void SaveSettings()
        => App.Settings.SetString("SerializedHotkeys", SerializedHotkeysTextBox.Text ?? string.Empty);

    private void Reset_Click(object? sender, RoutedEventArgs e)
    {
        SerializedHotkeysTextBox.Text = string.Empty;
    }
}
