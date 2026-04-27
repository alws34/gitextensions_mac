using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace GitUI.Avalonia.CommitDetails;

public enum DiffLineType
{
    Added,
    Removed,
    Section,
    FileHeader,
    Context,
}

public sealed class DiffLineColorizer : DocumentColorizingTransformer
{
    private static readonly IBrush AddedBg = new SolidColorBrush(Color.FromArgb(55, 0, 180, 0));
    private static readonly IBrush RemovedBg = new SolidColorBrush(Color.FromArgb(55, 200, 0, 0));
    private static readonly IBrush SectionBg = new SolidColorBrush(Color.FromArgb(40, 180, 180, 0));

    public static DiffLineType GetLineType(string line)
    {
        if (line.Length == 0)
        {
            return DiffLineType.Context;
        }

        if (line.StartsWith("+++") || line.StartsWith("---"))
        {
            return DiffLineType.FileHeader;
        }

        if (line[0] == '+')
        {
            return DiffLineType.Added;
        }

        if (line[0] == '-')
        {
            return DiffLineType.Removed;
        }

        if (line.StartsWith("@@"))
        {
            return DiffLineType.Section;
        }

        return DiffLineType.Context;
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (line.Length == 0)
        {
            return;
        }

        string text = CurrentContext.Document.GetText(line.Offset, Math.Min(line.Length, 3));
        IBrush? bg = GetLineType(text) switch
        {
            DiffLineType.Added => AddedBg,
            DiffLineType.Removed => RemovedBg,
            DiffLineType.Section => SectionBg,
            _ => null,
        };

        if (bg is null)
        {
            return;
        }

        ChangeLinePart(line.Offset, line.EndOffset, el => el.BackgroundBrush = bg);
    }
}
