using GitUI.Avalonia.RevisionGrid.Graph;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.RevisionGrid;

public sealed class RevisionRow
{
    public RevisionRow(
        GitRevision revision,
        IRevisionGraphRow? graphRow,
        IRevisionGraphRow? prevRow,
        IRevisionGraphRow? nextRow)
    {
        Revision = revision;
        GraphRow = graphRow;
        PrevRow = prevRow;
        NextRow = nextRow;
    }

    public GitRevision Revision { get; }
    public IRevisionGraphRow? GraphRow { get; }
    public IRevisionGraphRow? PrevRow { get; }
    public IRevisionGraphRow? NextRow { get; }

    public string Subject => Revision.Subject ?? string.Empty;
    public string Author => Revision.Author ?? string.Empty;
    public DateTimeOffset AuthorDate => DateTimeOffset.FromUnixTimeSeconds(Revision.AuthorUnixTime);
}
