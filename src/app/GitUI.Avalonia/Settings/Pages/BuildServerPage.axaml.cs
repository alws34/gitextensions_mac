using Avalonia.Controls;
using GitUI.Avalonia.Settings;

namespace GitUI.Avalonia.Settings.Pages;

public partial class BuildServerPage : UserControl, ISettingsPage
{
    private static readonly string[] BuildServerTypeTags =
    [
        string.Empty,
        "AppVeyor",
        "Azure DevOps and Team Foundation Server (since TFS2015)",
        "GitHub Actions",
        "Gitlab",
        "Jenkins",
        "TeamCity",
        "custom"
    ];

    public BuildServerPage()
    {
        InitializeComponent();
        Load();
        BuildServerTypeComboBox.SelectionChanged += (_, _) => UpdateCustomTypeVisibility();
    }

    private void Load()
    {
        EnableIntegrationCheckBox.IsChecked = App.Settings.GetBool("BuildServer.EnableIntegration", false);
        ShowBuildResultPageCheckBox.IsChecked = App.Settings.GetBool("BuildServer.ShowBuildResultPage", false);
        string serverType = App.Settings.GetString("BuildServer.Type", string.Empty);
        BuildServerTypeComboBox.SelectedIndex = IndexOfTag(serverType);
        CustomBuildServerTypeTextBox.Text = IsKnownType(serverType) ? string.Empty : serverType;
        PrioritizedRemotesTextBox.Text = App.Settings.GetString("PrioritizedBuildServerRemoteNames", "upstream|origin|remote");
        UpdateCustomTypeVisibility();
    }

    public void SaveSettings()
    {
        App.Settings.SetBool("BuildServer.EnableIntegration", EnableIntegrationCheckBox.IsChecked == true);
        App.Settings.SetBool("BuildServer.ShowBuildResultPage", ShowBuildResultPageCheckBox.IsChecked == true);
        App.Settings.SetString("BuildServer.Type", SelectedServerType());
        App.Settings.SetString("PrioritizedBuildServerRemoteNames", PrioritizedRemotesTextBox.Text?.Trim() ?? "upstream|origin|remote");
    }

    private string SelectedServerType()
    {
        if (TagAtIndex(BuildServerTypeComboBox.SelectedIndex) == "custom")
        {
            return CustomBuildServerTypeTextBox.Text?.Trim() ?? string.Empty;
        }

        return TagAtIndex(BuildServerTypeComboBox.SelectedIndex);
    }

    private void UpdateCustomTypeVisibility()
        => CustomBuildServerTypeRow.IsVisible = TagAtIndex(BuildServerTypeComboBox.SelectedIndex) == "custom";

    private static bool IsKnownType(string tag)
        => BuildServerTypeTags.Contains(tag);

    private static int IndexOfTag(string tag)
    {
        int idx = Array.IndexOf(BuildServerTypeTags, tag);
        return idx < 0 ? BuildServerTypeTags.Length - 1 : idx;
    }

    private static string TagAtIndex(int index)
    {
        if (index < 0 || index >= BuildServerTypeTags.Length)
        {
            return string.Empty;
        }

        return BuildServerTypeTags[index];
    }
}
