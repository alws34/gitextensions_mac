using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GitUI.Avalonia.RevisionGrid.Graph;

namespace GitUI.Avalonia.RevisionGrid;

public sealed class GraphCell : Control
{
    public static readonly StyledProperty<IRevisionGraphRow?> RowProperty =
        AvaloniaProperty.Register<GraphCell, IRevisionGraphRow?>(nameof(Row));

    public static readonly StyledProperty<IRevisionGraphRow?> PrevRowProperty =
        AvaloniaProperty.Register<GraphCell, IRevisionGraphRow?>(nameof(PrevRow));

    public static readonly StyledProperty<IRevisionGraphRow?> NextRowProperty =
        AvaloniaProperty.Register<GraphCell, IRevisionGraphRow?>(nameof(NextRow));

    public IRevisionGraphRow? Row
    {
        get => GetValue(RowProperty);
        set => SetValue(RowProperty, value);
    }

    public IRevisionGraphRow? PrevRow
    {
        get => GetValue(PrevRowProperty);
        set => SetValue(PrevRowProperty, value);
    }

    public IRevisionGraphRow? NextRow
    {
        get => GetValue(NextRowProperty);
        set => SetValue(NextRowProperty, value);
    }

    static GraphCell()
    {
        AffectsRender<GraphCell>(RowProperty, PrevRowProperty, NextRowProperty);
    }

    public override void Render(DrawingContext context)
    {
        GraphRenderer.RenderGraphCell(context, Row, PrevRow, NextRow, Bounds.Width, Bounds.Height);
    }
}
