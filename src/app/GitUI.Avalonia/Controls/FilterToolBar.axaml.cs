using Avalonia.Controls;

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
    public event Action<string, FilterType>? FilterChanged;

    public FilterToolBar()
    {
        InitializeComponent();
    }

    private void FilterBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        string text = FilterBox.Text ?? string.Empty;
        FilterType type = FilterTypeCombo.SelectedIndex switch
        {
            1 => FilterType.Author,
            2 => FilterType.Message,
            3 => FilterType.Hash,
            _ => FilterType.All,
        };
        FilterChanged?.Invoke(text, type);
    }

    private void Clear_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        FilterBox.Text = string.Empty;
    }
}
