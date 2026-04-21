using Avalonia.Controls;
using Avalonia.Interactivity;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class AboutDialog : GitExtensionsDialog
{
    public AboutDialog()
    {
        InitializeComponent();
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        VersionLabel.Text = $"Version {version?.Major}.{version?.Minor}.{version?.Build}";
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close(null);
}
