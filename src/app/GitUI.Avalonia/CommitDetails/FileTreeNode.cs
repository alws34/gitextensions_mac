namespace GitUI.Avalonia.CommitDetails;

public sealed class FileTreeNode
{
    public string Name { get; }
    public bool IsDirectory { get; }
    public string? FilePath { get; }
    public FileStatusItem? FileItem { get; }
    public List<FileTreeNode> Children { get; } = [];

    public string DisplayIcon => IsDirectory ? "▶" : (FileItem?.StatusIcon ?? "");

    public string StatusColor => FileItem?.StatusIcon switch
    {
        "A" => "#FF22AA22",
        "D" => "#FFCC2222",
        "R" => "#FF8822CC",
        "M" => "#FFCC6600",
        _ => "#FF666666",
    };

    private FileTreeNode(string name, bool isDirectory, string? filePath = null, FileStatusItem? item = null)
    {
        Name = name;
        IsDirectory = isDirectory;
        FilePath = filePath;
        FileItem = item;
    }

    public static IReadOnlyList<FileTreeNode> BuildTree(IEnumerable<FileStatusItem> files)
    {
        var rootChildren = new Dictionary<string, FileTreeNode>(StringComparer.Ordinal);

        foreach (FileStatusItem file in files)
        {
            string[] parts = file.Name.Replace('\\', '/').Split('/');
            InsertFile(rootChildren, parts, 0, file);
        }

        return SortNodes(rootChildren.Values);
    }

    public static IReadOnlyList<FileTreeNode> BuildFlat(IEnumerable<FileStatusItem> files) =>
        [.. files
            .OrderBy(file => file.Name, StringComparer.Ordinal)
            .Select(file => new FileTreeNode(file.Name, isDirectory: false, file.Name, file))];

    public static IReadOnlyList<FileTreeNode> BuildGrouped(IEnumerable<FileStatusItem> files)
    {
        return [.. files
            .GroupBy(file => file.StatusGroup)
            .OrderBy(group => GroupSortKey(group.Key))
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var node = new FileTreeNode(group.Key, isDirectory: true);
                node.Children.AddRange(BuildFlat(group));
                return node;
            })];
    }

    private static void InsertFile(
        Dictionary<string, FileTreeNode> children,
        string[] parts,
        int depth,
        FileStatusItem file)
    {
        string name = parts[depth];

        if (depth == parts.Length - 1)
        {
            children[name] = new FileTreeNode(name, isDirectory: false, file.Name, file);
            return;
        }

        if (!children.TryGetValue(name, out FileTreeNode? dir))
        {
            dir = new FileTreeNode(name, isDirectory: true);
            children[name] = dir;
        }

        var childDict = dir.Children.ToDictionary(c => c.Name, StringComparer.Ordinal);
        InsertFile(childDict, parts, depth + 1, file);
        dir.Children.Clear();
        foreach (FileTreeNode child in SortNodes(childDict.Values))
        {
            dir.Children.Add(child);
        }
    }

    private static IReadOnlyList<FileTreeNode> SortNodes(IEnumerable<FileTreeNode> nodes) =>
        [.. nodes.OrderBy(n => !n.IsDirectory).ThenBy(n => n.Name)];

    private static int GroupSortKey(string group) => group switch
    {
        "Staged" => 0,
        "Staged and unstaged" => 1,
        "Unstaged" => 2,
        _ => 3,
    };
}
