using Avalonia.Controls;

namespace GitExtensions.Plugins.GitStatistics;

public partial class GitStatisticsAvaloniaSettings : Avalonia.Controls.UserControl
{
    public GitStatisticsAvaloniaSettings()
    {
        InitializeComponent();
        CodeFilesBox.Text = "*.c;*.cpp;*.cs;*.ts;*.js;*.py;*.rb;*.java;*.go;*.rs;*.fs;*.fsx";
        IgnoreDirsBox.Text = @"\Debug;\Release;\obj;\bin;\lib";
    }
}
