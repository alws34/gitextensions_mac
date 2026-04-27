namespace GitUI.Avalonia.CommitDetails;

public sealed class CommitTreeNode
{
    public string Name { get; }
    public bool IsDirectory { get; }
    public List<CommitTreeNode> Children { get; } = [];

    public CommitTreeNode(string name, bool isDirectory)
    {
        Name = name;
        IsDirectory = isDirectory;
    }

    public static IReadOnlyList<CommitTreeNode> BuildTree(IEnumerable<string> paths)
    {
        var root = new CommitTreeNode("root", isDirectory: true);
        foreach (string path in paths)
        {
            string[] parts = path.Split('/');
            CommitTreeNode current = root;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                CommitTreeNode? dir = current.Children
                    .FirstOrDefault(c => c.IsDirectory && c.Name == parts[i]);
                if (dir is null)
                {
                    dir = new CommitTreeNode(parts[i], isDirectory: true);
                    current.Children.Add(dir);
                }

                current = dir;
            }

            current.Children.Add(new CommitTreeNode(parts[^1], isDirectory: false));
        }

        SortChildren(root);
        return root.Children;
    }

    private static void SortChildren(CommitTreeNode node)
    {
        node.Children.Sort((a, b) =>
        {
            if (a.IsDirectory != b.IsDirectory)
            {
                return a.IsDirectory ? -1 : 1;
            }

            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });
        foreach (CommitTreeNode child in node.Children.Where(c => c.IsDirectory))
        {
            SortChildren(child);
        }
    }
}
