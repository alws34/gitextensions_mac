namespace GitCommands.DiffMergeTools;

/// <summary>
/// Apple FileMerge (ships with Xcode Command Line Tools).
/// Launched via the opendiff command-line tool.
/// </summary>
internal class FileMerge : DiffMergeTool
{
    /// <inheritdoc />
    public override string DiffCommand => "\"$LOCAL\" \"$REMOTE\"";

    /// <inheritdoc />
    public override string ExeFileName => "opendiff";

    /// <inheritdoc />
    public override string MergeCommand => "\"$LOCAL\" \"$REMOTE\" -ancestor \"$BASE\" -merge \"$MERGED\"";

    /// <inheritdoc />
    public override string Name => "opendiff";

    /// <inheritdoc />
    public override IEnumerable<string> SearchPaths =>
    [
        "/usr/bin/opendiff"
    ];
}
