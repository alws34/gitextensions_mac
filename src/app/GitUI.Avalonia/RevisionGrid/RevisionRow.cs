using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.RevisionGrid.Graph;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.RevisionGrid;

public sealed class RevisionRow
{
    // Normal commit row
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

    // Artificial row (Working Tree / Index)
    public RevisionRow(string objectId, string subject, string artificialType)
    {
        Revision = null!;
        GraphRow = null;
        PrevRow = null;
        NextRow = null;
        IsArtificial = true;
        ArtificialType = artificialType;
        _artificialObjectId = objectId;
        _artificialSubject = subject;
    }

    private readonly string? _artificialObjectId;
    private readonly string? _artificialSubject;

    public GitRevision? Revision { get; }
    public IRevisionGraphRow? GraphRow { get; }
    public IRevisionGraphRow? PrevRow { get; }
    public IRevisionGraphRow? NextRow { get; }

    // Artificial row flags
    public bool IsArtificial { get; init; }
    public string ArtificialType { get; init; } = string.Empty;  // "Index" or "WorkTree"

    // HEAD indicator
    public bool IsCurrent { get; init; }

    // Windows-style all-branches view dims rows outside the selected/current branch ancestry.
    public bool IsRelativeToCurrentBranch { get; init; } = true;

    // Tooltip body (populated lazily)
    public string CommitBody { get; set; } = string.Empty;

    public string Subject => IsArtificial ? (_artificialSubject ?? string.Empty) : (Revision?.Subject ?? string.Empty);
    public string Author => IsArtificial ? string.Empty : (Revision?.Author ?? string.Empty);
    public DateTimeOffset AuthorDate => IsArtificial ? DateTimeOffset.Now : DateTimeOffset.FromUnixTimeSeconds(Revision!.AuthorUnixTime);
    public string ShortHash => IsArtificial ? "--------" : (Revision?.ObjectId?.ToShortString() ?? string.Empty);
    public string RowBackground => IsCurrent ? "#220078D4" : "Transparent";
    public string TextColor => IsRelativeToCurrentBranch ? "#FF202020" : "#FF8A8A8A";
    public string HashColor => IsRelativeToCurrentBranch ? "#FF777777" : "#FFB0B0B0";

    public IReadOnlyList<IGitRef> Refs => (IsArtificial ? null : Revision?.Refs) ?? [];

    public string RefsText => Refs.Count == 0
        ? string.Empty
        : string.Join("  ", Refs.Select(r => r.LocalName));

    public string RelativeDate => IsArtificial ? string.Empty : FormatRelative(AuthorDate);

    private static string FormatRelative(DateTimeOffset date)
    {
        TimeSpan ago = DateTimeOffset.Now - date;
        if (ago.TotalMinutes < 1)
        {
            return "just now";
        }

        if (ago.TotalHours < 1)
        {
            return $"{(int)ago.TotalMinutes}m ago";
        }

        if (ago.TotalDays < 1)
        {
            return $"{(int)ago.TotalHours}h ago";
        }

        if (ago.TotalDays < 7)
        {
            return $"{(int)ago.TotalDays}d ago";
        }

        if (ago.TotalDays < 30)
        {
            return $"{(int)(ago.TotalDays / 7)}w ago";
        }

        if (ago.TotalDays < 365)
        {
            return $"{(int)(ago.TotalDays / 30)}mo ago";
        }

        return $"{(int)(ago.TotalDays / 365)}y ago";
    }
}
