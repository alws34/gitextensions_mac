using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GitUI.Avalonia.RevisionGrid.Graph;

namespace GitUI.Avalonia.RevisionGrid;

public sealed class GraphCell : Control
{
    private static readonly IReadOnlyList<Color> LaneColors =
    [
        Colors.SteelBlue, Colors.OrangeRed, Colors.MediumSeaGreen, Colors.Orchid,
        Colors.Gold, Colors.DodgerBlue, Colors.Tomato, Colors.MediumAquamarine,
        Colors.SlateBlue, Colors.Coral, Colors.CadetBlue, Colors.PaleVioletRed,
    ];

    public static readonly StyledProperty<IRevisionGraphRow?> RowProperty =
        AvaloniaProperty.Register<GraphCell, IRevisionGraphRow?>(nameof(Row));

    public IRevisionGraphRow? Row
    {
        get => GetValue(RowProperty);
        set => SetValue(RowProperty, value);
    }

    static GraphCell()
    {
        AffectsRender<GraphCell>(RowProperty);
    }

    public override void Render(DrawingContext context)
    {
        if (Row is not null)
        {
            GraphRenderer.RenderGraphCell(context, Row, Bounds.Width, Bounds.Height, LaneColors);
        }
    }
}
