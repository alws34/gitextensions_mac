namespace GitCommands.DiffMergeTools;

/// <summary>
/// Kaleidoscope diff/merge tool for macOS.
/// </summary>
internal class Kaleidoscope : DiffMergeTool
{
    /// <inheritdoc />
    public override string DiffCommand => "\"$LOCAL\" \"$REMOTE\"";

    /// <inheritdoc />
    public override string ExeFileName => "ksdiff";

    /// <inheritdoc />
    public override string MergeCommand => "--merge --output \"$MERGED\" --base \"$BASE\" -- \"$LOCAL\" \"$REMOTE\"";

    /// <inheritdoc />
    public override string Name => "kaleidoscope";

    /// <inheritdoc />
    public override IEnumerable<string> SearchPaths =>
    [
        "/usr/local/bin/ksdiff",
        "/opt/homebrew/bin/ksdiff"
    ];
}
