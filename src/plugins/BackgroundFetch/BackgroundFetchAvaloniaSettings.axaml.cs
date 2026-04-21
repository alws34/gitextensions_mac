using Avalonia.Controls;

namespace GitExtensions.Plugins.BackgroundFetch;

public partial class BackgroundFetchAvaloniaSettings : Avalonia.Controls.UserControl
{
    public BackgroundFetchAvaloniaSettings()
    {
        InitializeComponent();
        GitCommandBox.Text = "fetch --all";
    }
}
