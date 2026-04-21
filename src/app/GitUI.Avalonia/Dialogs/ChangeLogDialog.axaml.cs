using Avalonia.Controls;
using Avalonia.Interactivity;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class ChangeLogDialog : GitExtensionsDialog
{
    public ChangeLogDialog()
    {
        InitializeComponent();
        _ = LoadChangelogAsync();
    }

    private async Task LoadChangelogAsync()
    {
        string content = await Task.Run(() =>
        {
            // Look for CHANGELOG.md from the executable directory upward
            string dir = AppContext.BaseDirectory;
            for (int i = 0; i < 6; i++)
            {
                string candidate = System.IO.Path.Combine(dir, "CHANGELOG.md");
                if (System.IO.File.Exists(candidate))
                {
                    return System.IO.File.ReadAllText(candidate);
                }

                dir = System.IO.Path.GetDirectoryName(dir) ?? dir;
            }

            return "No CHANGELOG.md found.";
        });

        ChangeLogEditor.Text = content;
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close(null);
}
