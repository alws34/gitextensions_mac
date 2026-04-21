using Avalonia.Controls;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class BlameDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private readonly string _filePath;

    public BlameDialog(GitModule module, string filePath)
    {
        _module = module;
        _filePath = filePath;
        InitializeComponent();
        Title = $"Blame — {System.IO.Path.GetFileName(filePath)}";
        _ = LoadBlameAsync();
    }

    private async Task LoadBlameAsync()
    {
        var (annotations, lines) = await Task.Run(() =>
        {
            string output = _module.GitExecutable.GetOutput($"blame --line-porcelain -- \"{_filePath}\"");
            var blameLines = output.Split('\n');
            var annots = new List<string>();
            var fileLines = new List<string>();
            string currentHash = string.Empty;
            string currentAuthor = string.Empty;

            foreach (string line in blameLines)
            {
                if (line.Length >= 40 && line[0] != '\t' && !line.StartsWith("author", StringComparison.Ordinal) &&
                    !line.StartsWith("summary", StringComparison.Ordinal) &&
                    !line.StartsWith("filename", StringComparison.Ordinal) &&
                    !line.StartsWith("committer", StringComparison.Ordinal) &&
                    !line.StartsWith("previous", StringComparison.Ordinal) &&
                    !line.StartsWith("boundary", StringComparison.Ordinal))
                {
                    currentHash = line.Split(' ')[0][..7];
                }
                else if (line.StartsWith("author ", StringComparison.Ordinal))
                {
                    currentAuthor = line[7..];
                }
                else if (line.StartsWith("\t", StringComparison.Ordinal))
                {
                    annots.Add($"{currentHash} {currentAuthor,-20}");
                    fileLines.Add(line[1..]);
                }
            }

            return (annots, fileLines);
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            BlameList.ItemsSource = annotations;
            BlameEditor.Text = string.Join('\n', lines);
        });
    }
}
