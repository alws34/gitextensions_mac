using System.Reflection;
using Avalonia.Controls;
using GitUI.Avalonia.Infrastructure;
using GitUI.Avalonia.Settings.Pages;
using NUnit.Framework;
using AvaloniaApp = GitUI.Avalonia.App;

namespace GitUI.Avalonia.Tests.Settings;

[TestFixture]
public class SettingsPagesTests
{
    private InMemorySettingsBackend _settings = null!;

    [SetUp]
    public void SetUp()
    {
        AvaloniaTestHost.EnsureStarted();
        _settings = new InMemorySettingsBackend();
        SetAppSettings(_settings);
    }

    [Test]
    public void ConfirmationsPage_SaveSettings_WritesWindowsConfirmationKeys()
    {
        ConfirmationsPage page = new();

        page.FindControl<CheckBox>("ConfirmAmendCheckBox")!.IsChecked = false;
        page.FindControl<CheckBox>("ConfirmBranchCheckoutCheckBox")!.IsChecked = true;
        page.FindControl<CheckBox>("ConfirmSwitchWorktreeCheckBox")!.IsChecked = false;

        page.SaveSettings();

        Assert.Multiple(() =>
        {
            Assert.That(_settings.GetBool("DontConfirmAmend", false), Is.True);
            Assert.That(_settings.GetBool("Confirmations.ConfirmBranchCheckout", false), Is.True);
            Assert.That(_settings.GetBool("DontConfirmSwitchWorktree", false), Is.True);
        });
    }

    [Test]
    public void BuildServerPage_SaveSettings_WritesBuildServerKeys()
    {
        BuildServerPage page = new();

        page.FindControl<CheckBox>("EnableIntegrationCheckBox")!.IsChecked = true;
        page.FindControl<CheckBox>("ShowBuildResultPageCheckBox")!.IsChecked = true;
        page.FindControl<ComboBox>("BuildServerTypeComboBox")!.SelectedIndex = 4;
        page.FindControl<TextBox>("PrioritizedRemotesTextBox")!.Text = "upstream|origin";

        page.SaveSettings();

        Assert.Multiple(() =>
        {
            Assert.That(_settings.GetBool("BuildServer.EnableIntegration", false), Is.True);
            Assert.That(_settings.GetBool("BuildServer.ShowBuildResultPage", false), Is.True);
            Assert.That(_settings.GetString("BuildServer.Type", ""), Is.EqualTo("Gitlab"));
            Assert.That(_settings.GetString("PrioritizedBuildServerRemoteNames", ""), Is.EqualTo("upstream|origin"));
        });
    }

    [Test]
    public void TextBackedPages_SaveSettings_WriteExpectedKeys()
    {
        HotkeysPage hotkeys = new();
        ScriptsPage scripts = new();
        RevisionLinksPage revisionLinks = new();

        hotkeys.FindControl<TextBox>("SerializedHotkeysTextBox")!.Text = "<hotkeys />";
        scripts.FindControl<TextBox>("OwnScriptsTextBox")!.Text = "<scripts />";
        revisionLinks.FindControl<TextBox>("RevisionLinkDefsTextBox")!.Text = "<links />";

        hotkeys.SaveSettings();
        scripts.SaveSettings();
        revisionLinks.SaveSettings();

        Assert.Multiple(() =>
        {
            Assert.That(_settings.GetString("SerializedHotkeys", ""), Is.EqualTo("<hotkeys />"));
            Assert.That(_settings.GetString("ownScripts", ""), Is.EqualTo("<scripts />"));
            Assert.That(_settings.GetString("RevisionLinkDefs", ""), Is.EqualTo("<links />"));
        });
    }

    [Test]
    public void PluginsPage_SaveSettings_WritesPluginConfigurationKeys()
    {
        PluginsPage page = new();

        page.FindControl<CheckBox>("EnablePluginsCheckBox")!.IsChecked = false;
        page.FindControl<TextBox>("UserPluginsPathTextBox")!.Text = "/tmp/gitextensions-plugins";
        page.FindControl<TextBox>("PluginPatternTextBox")!.Text = "GitExtensions.Custom.*.dll";

        page.SaveSettings();

        Assert.Multiple(() =>
        {
            Assert.That(_settings.GetBool("enablePlugins", true), Is.False);
            Assert.That(_settings.GetString("userPluginsPath", ""), Is.EqualTo("/tmp/gitextensions-plugins"));
            Assert.That(_settings.GetString("pluginScanPattern", ""), Is.EqualTo("GitExtensions.Custom.*.dll"));
        });
    }

    private static void SetAppSettings(ISettingsBackend settings)
        => typeof(AvaloniaApp)
            .GetProperty(nameof(AvaloniaApp.Settings), BindingFlags.Public | BindingFlags.Static)!
            .GetSetMethod(nonPublic: true)!
            .Invoke(null, [settings]);

    private sealed class InMemorySettingsBackend : ISettingsBackend
    {
        private readonly Dictionary<string, object> _values = [];

        public string GetString(string key, string defaultValue)
            => _values.TryGetValue(key, out object? value) && value is string stringValue
                ? stringValue
                : defaultValue;

        public void SetString(string key, string value)
            => _values[key] = value;

        public bool GetBool(string key, bool defaultValue)
            => _values.TryGetValue(key, out object? value) && value is bool boolValue
                ? boolValue
                : defaultValue;

        public void SetBool(string key, bool value)
            => _values[key] = value;

        public int GetInt(string key, int defaultValue)
            => _values.TryGetValue(key, out object? value) && value is int intValue
                ? intValue
                : defaultValue;

        public void SetInt(string key, int value)
            => _values[key] = value;

        public IReadOnlyList<string> GetStringList(string key)
            => _values.TryGetValue(key, out object? value) && value is IReadOnlyList<string> list
                ? list
                : [];

        public void SetStringList(string key, IEnumerable<string> values)
            => _values[key] = values.ToArray();

        public void Save()
        {
        }
    }
}
