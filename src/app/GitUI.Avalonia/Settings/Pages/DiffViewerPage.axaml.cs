using Avalonia.Controls;
using GitUI.Avalonia.Settings;

namespace GitUI.Avalonia.Settings.Pages;

public partial class DiffViewerPage : UserControl, ISettingsPage
{
    public DiffViewerPage()
    {
        InitializeComponent();
        SplitViewByDefaultCheckBox.IsChecked = App.Settings.GetBool("diffSplitViewDefault", false);
        SyntaxHighlightingCheckBox.IsChecked = App.Settings.GetBool("diffSyntaxHighlighting", true);
        WordWrapCheckBox.IsChecked = App.Settings.GetBool("diffWordWrap", false);
    }

    public void SaveSettings()
    {
        App.Settings.SetBool("diffSplitViewDefault", SplitViewByDefaultCheckBox.IsChecked == true);
        App.Settings.SetBool("diffSyntaxHighlighting", SyntaxHighlightingCheckBox.IsChecked == true);
        App.Settings.SetBool("diffWordWrap", WordWrapCheckBox.IsChecked == true);
    }
}
