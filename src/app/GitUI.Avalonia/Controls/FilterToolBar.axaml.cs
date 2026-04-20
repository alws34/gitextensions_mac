using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GitUI.Avalonia.Controls;

public partial class FilterToolBar : UserControl
{
    public event Action<string>? BranchFilterChanged;
    public event Action<string>? MessageFilterChanged;

    public FilterToolBar()
    {
        InitializeComponent();
        BranchFilter.TextChanged += (_, _) => BranchFilterChanged?.Invoke(BranchFilter.Text ?? "");
        MessageFilter.TextChanged += (_, _) => MessageFilterChanged?.Invoke(MessageFilter.Text ?? "");
    }

    private void ClearFilter_Click(object? sender, RoutedEventArgs e)
    {
        BranchFilter.Text = "";
        MessageFilter.Text = "";
    }
}
