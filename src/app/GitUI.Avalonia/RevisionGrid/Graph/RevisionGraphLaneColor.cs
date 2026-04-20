using Avalonia.Media;

namespace GitUI.Avalonia.RevisionGrid.Graph;

public static class RevisionGraphLaneColor
{
    public static readonly Color NonRelativeColor = Colors.Gray;

    internal static readonly IBrush NonRelativeBrush = new SolidColorBrush(NonRelativeColor);

    internal static readonly List<IBrush> PresetGraphBrushes =
    [
        new SolidColorBrush(Color.FromRgb(0x44, 0x78, 0xC8)),
        new SolidColorBrush(Color.FromRgb(0xC8, 0x44, 0x44)),
        new SolidColorBrush(Color.FromRgb(0x44, 0xC8, 0x44)),
        new SolidColorBrush(Color.FromRgb(0xC8, 0xC8, 0x44)),
        new SolidColorBrush(Color.FromRgb(0x44, 0xC8, 0xC8)),
        new SolidColorBrush(Color.FromRgb(0xC8, 0x44, 0xC8)),
        new SolidColorBrush(Color.FromRgb(0xFF, 0x88, 0x44)),
        new SolidColorBrush(Color.FromRgb(0x88, 0xFF, 0x44)),
    ];

    public static int GetColorForLane(int seed) => Math.Abs(seed) % PresetGraphBrushes.Count;

    public static IBrush GetBrushForLane(int laneColor) => PresetGraphBrushes[laneColor];
}
