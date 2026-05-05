using Avalonia.Controls;
using GitUI.Avalonia.Settings;

namespace GitUI.Avalonia.Settings.Pages;

public partial class RevisionGridPage : UserControl, ISettingsPage
{
    public RevisionGridPage()
    {
        InitializeComponent();
        ShowTagsCheckBox.IsChecked = App.Settings.GetBool("showTags", true);
        ShowStashesCheckBox.IsChecked = App.Settings.GetBool("showStashesInGraph", false);
        ShowWorktreesCheckBox.IsChecked = App.Settings.GetBool("showWorktreesInGraph", false);
        FirstParentOnlyCheckBox.IsChecked = App.Settings.GetBool("showFirstParentOnly", false);
        MaxRevisionsBox.Value = App.Settings.GetInt("revisionGridMaxRevisions", 2000);
    }

    public void SaveSettings()
    {
        App.Settings.SetBool("showTags", ShowTagsCheckBox.IsChecked == true);
        App.Settings.SetBool("showStashesInGraph", ShowStashesCheckBox.IsChecked == true);
        App.Settings.SetBool("showWorktreesInGraph", ShowWorktreesCheckBox.IsChecked == true);
        App.Settings.SetBool("showFirstParentOnly", FirstParentOnlyCheckBox.IsChecked == true);
        App.Settings.SetInt("revisionGridMaxRevisions", (int)(MaxRevisionsBox.Value ?? 2000));
    }
}
