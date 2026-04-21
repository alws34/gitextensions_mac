using GitUI.Avalonia.RevisionGrid.Graph;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.RevisionGrid;

public sealed class RevisionRow
{
    public RevisionRow(GitRevision revision, IRevisionGraphRow? graphRow)
    {
        Revision = revision;
        GraphRow = graphRow;
    }

    public GitRevision Revision { get; }
    public IRevisionGraphRow? GraphRow { get; }

    public string Subject => Revision.Subject ?? string.Empty;
    public string Author => Revision.Author ?? string.Empty;
    public DateTimeOffset AuthorDate => DateTimeOffset.FromUnixTimeSeconds(Revision.AuthorUnixTime);
}
