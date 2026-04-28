using Avalonia.Controls;
using Avalonia.Threading;

namespace GitUI.Avalonia.Controls;

public enum FilterType
{
    All,
    Author,
    Message,
    Hash,
}

public partial class FilterToolBar : UserControl
{
    private System.Threading.Timer? _debounceTimer;

    public event Action<string, FilterType>? FilterChanged;

    public FilterToolBar()
    {
        InitializeComponent();
    }

    private FilterType CurrentFilterType => FilterTypeCombo.SelectedIndex switch
    {
        1 => FilterType.Author,
        2 => FilterType.Message,
        3 => FilterType.Hash,
        _ => FilterType.All,
    };

    private void FilterBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Threading.Timer(
            _ =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    string text = FilterBox.Text ?? string.Empty;
                    FilterChanged?.Invoke(text, CurrentFilterType);
                });
            },
            null,
            dueTime: 300,
            period: System.Threading.Timeout.Infinite);
    }

    private void Clear_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        FilterBox.Text = string.Empty;
    }
}
