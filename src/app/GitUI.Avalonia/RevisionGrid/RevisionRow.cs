using GitExtensions.Extensibility.Git;
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
    public string ShortHash => Revision.ObjectId.ToShortString();

    public IReadOnlyList<IGitRef> Refs => Revision.Refs ?? [];

    public string RefsText => Refs.Count == 0
        ? string.Empty
        : string.Join("  ", Refs.Select(r => r.LocalName));
}
