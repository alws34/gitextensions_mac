using Avalonia.Controls;

namespace GitUI.Avalonia.Controls;

public partial class StatusOutputLog : UserControl
{
    public StatusOutputLog()
    {
        InitializeComponent();
    }

    public void AppendLine(string line)
    {
        OutputText.Text += line + "\n";
        Scroller.ScrollToEnd();
    }

    public void Clear()
    {
        OutputText.Text = string.Empty;
    }
}
