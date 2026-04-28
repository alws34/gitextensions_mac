using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitUI.Avalonia.Settings;

namespace GitUI.Avalonia.Settings.Pages;

public partial class CredentialsPage : UserControl, ISettingsPage
{
    private static readonly string[] HelperTags = ["", "osxkeychain", "store", "manager"];

    public CredentialsPage()
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        string helper = App.Settings.GetString("credentialHelper", "osxkeychain");
        CredentialHelperComboBox.SelectedIndex = IndexOfTag(helper);
        UsernameTextBox.Text = App.Settings.GetString("defaultUsername", string.Empty);
    }

    public void SaveSettings()
    {
        App.Settings.SetString("credentialHelper", TagAtIndex(CredentialHelperComboBox.SelectedIndex));
        App.Settings.SetString("defaultUsername", UsernameTextBox.Text?.Trim() ?? string.Empty);
    }

    private void ConfigureNow_Click(object? sender, RoutedEventArgs e)
    {
        _ = ConfigureNowAsync();
    }

    private async Task ConfigureNowAsync()
    {
        string helper = TagAtIndex(CredentialHelperComboBox.SelectedIndex);
        try
        {
            string result = await Task.Run(() =>
            {
                var psi = new System.Diagnostics.ProcessStartInfo("git",
                    $"config --global credential.helper {helper}")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                using var proc = System.Diagnostics.Process.Start(psi);
                proc?.WaitForExit();
                string err = proc?.StandardError.ReadToEnd().Trim() ?? string.Empty;
                return string.IsNullOrEmpty(err) ? "Done." : err;
            });
            await Dispatcher.UIThread.InvokeAsync(() => ConfigureResultLabel.Text = result);
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => ConfigureResultLabel.Text = $"Error: {ex.Message}");
        }
    }

    private static int IndexOfTag(string tag)
    {
        int idx = Array.IndexOf(HelperTags, tag);
        return idx < 0 ? 1 : idx; // default to osxkeychain (index 1)
    }

    private static string TagAtIndex(int index)
    {
        if (index < 0 || index >= HelperTags.Length)
        {
            return "osxkeychain";
        }

        return HelperTags[index];
    }
}
