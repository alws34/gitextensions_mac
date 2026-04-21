using Avalonia.Controls;

namespace GitExtensions.Plugins.FindLargeFiles;

public partial class FindLargeFilesAvaloniaSettings : UserControl
{
    public FindLargeFilesAvaloniaSettings()
    {
        InitializeComponent();
    }

    public double SizeLimitMb => SizeUpDown.Value ?? 1.0;
}
