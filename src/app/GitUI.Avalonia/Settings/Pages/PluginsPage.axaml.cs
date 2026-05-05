using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GitUI.Avalonia.Settings;

namespace GitUI.Avalonia.Settings.Pages;

public partial class PluginsPage : UserControl, ISettingsPage
{
    public PluginsPage()
    {
        InitializeComponent();
        EnablePluginsCheckBox.IsChecked = App.Settings.GetBool("enablePlugins", true);
        UserPluginsPathTextBox.Text = App.Settings.GetString("userPluginsPath", DefaultUserPluginsPath());
        PluginPatternTextBox.Text = App.Settings.GetString("pluginScanPattern", "GitExtensions.*.dll");
    }

    public void SaveSettings()
    {
        App.Settings.SetBool("enablePlugins", EnablePluginsCheckBox.IsChecked == true);
        App.Settings.SetString("userPluginsPath", UserPluginsPathTextBox.Text?.Trim() ?? DefaultUserPluginsPath());
        App.Settings.SetString("pluginScanPattern", PluginPatternTextBox.Text?.Trim() ?? "GitExtensions.*.dll");
    }

    private void Browse_Click(object? sender, RoutedEventArgs e)
    {
        _ = BrowseAsync();
    }

    private async Task BrowseAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select user plugins folder",
            AllowMultiple = false,
        });

        if (folders.Count > 0)
        {
            UserPluginsPathTextBox.Text = folders[0].Path.LocalPath;
        }
    }

    private void Open_Click(object? sender, RoutedEventArgs e)
    {
        string path = UserPluginsPathTextBox.Text?.Trim() ?? DefaultUserPluginsPath();
        Directory.CreateDirectory(path);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        });
    }

    private static string DefaultUserPluginsPath()
        => Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GitExtensions",
            "UserPlugins");
}
