using Avalonia.Controls;

namespace GitExtensions.Plugins.BackgroundFetch;

public partial class BackgroundFetchAvaloniaSettings : UserControl
{
    public BackgroundFetchAvaloniaSettings()
    {
        InitializeComponent();
        GitCommandBox.Text = "fetch --all";
    }
}
