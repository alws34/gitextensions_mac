namespace GitUI.Avalonia.CommitDetails;

/// <summary>Parses unified diff text into aligned left/right line pairs.</summary>
public static class SideBySideDiffParser
{
    /// <summary>
    /// Represents one aligned row in a side-by-side diff.
    /// Either side may be empty (null) to represent an insertion or deletion gap.
    /// </summary>
    public sealed record DiffRow(string? Left, string? Right);

    /// <summary>Parses a unified diff string into aligned left/right rows.</summary>
    public static IReadOnlyList<DiffRow> Parse(string unifiedDiff)
    {
        var rows = new List<DiffRow>();

        // Buffer removed lines until we see the matching added block
        var removedBuffer = new List<string>();

        foreach (string rawLine in unifiedDiff.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');

            if (line.StartsWith("---", StringComparison.Ordinal)
                || line.StartsWith("+++", StringComparison.Ordinal)
                || line.StartsWith("@@", StringComparison.Ordinal)
                || line.StartsWith("diff ", StringComparison.Ordinal)
                || line.StartsWith("index ", StringComparison.Ordinal))
            {
                // Flush buffer before header
                FlushRemoved(rows, removedBuffer);
                rows.Add(new DiffRow(line, line));
                continue;
            }

            if (line.StartsWith("-", StringComparison.Ordinal))
            {
                removedBuffer.Add(line[1..]);
            }
            else if (line.StartsWith("+", StringComparison.Ordinal))
            {
                string addedText = line[1..];
                if (removedBuffer.Count > 0)
                {
                    // Pair with a removed line
                    rows.Add(new DiffRow(removedBuffer[0], addedText));
                    removedBuffer.RemoveAt(0);
                }
                else
                {
                    rows.Add(new DiffRow(null, addedText));
                }
            }
            else
            {
                // Context line — flush remaining removed buffer first
                FlushRemoved(rows, removedBuffer);
                string contextText = line.Length > 0 ? line[1..] : string.Empty;
                rows.Add(new DiffRow(contextText, contextText));
            }
        }

        FlushRemoved(rows, removedBuffer);
        return rows;
    }

    private static void FlushRemoved(List<DiffRow> rows, List<string> removedBuffer)
    {
        foreach (string r in removedBuffer)
        {
            rows.Add(new DiffRow(r, null));
        }

        removedBuffer.Clear();
    }
}
