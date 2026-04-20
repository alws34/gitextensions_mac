using GitUI.Avalonia.Infrastructure;
using NUnit.Framework;

namespace GitUI.Avalonia.Tests.Infrastructure;

[TestFixture]
public class JsonSettingsBackendTests
{
    private string _tempDir = null!;
    private string _configPath = null!;
    private JsonSettingsBackend _backend = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Join(_tempDir, "settings.json");
        _backend = new JsonSettingsBackend(_configPath);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_tempDir, recursive: true);

    [Test]
    public void GetString_ReturnsDefault_WhenKeyMissing()
        => Assert.That(_backend.GetString("missingKey", "default"), Is.EqualTo("default"));

    [Test]
    public void SetString_ThenGetString_ReturnsSavedValue()
    {
        _backend.SetString("myKey", "myValue");
        Assert.That(_backend.GetString("myKey", "default"), Is.EqualTo("myValue"));
    }

    [Test]
    public void GetBool_ReturnsDefault_WhenKeyMissing()
        => Assert.That(_backend.GetBool("missing", defaultValue: true), Is.True);

    [Test]
    public void SetBool_ThenGetBool_ReturnsSavedValue()
    {
        _backend.SetBool("flag", false);
        Assert.That(_backend.GetBool("flag", defaultValue: true), Is.False);
    }

    [Test]
    public void Settings_PersistedToDisk_AfterSave()
    {
        _backend.SetString("persistKey", "persistValue");
        _backend.Save();
        JsonSettingsBackend reloaded = new(_configPath);
        Assert.That(reloaded.GetString("persistKey", "missing"), Is.EqualTo("persistValue"));
    }

    [Test]
    public void GetStringList_ReturnsEmpty_WhenKeyMissing()
        => Assert.That(_backend.GetStringList("paths"), Is.Empty);

    [Test]
    public void SetStringList_ThenGetStringList_ReturnsSavedList()
    {
        string[] paths = ["/Users/user/repo1", "/Users/user/repo2"];
        _backend.SetStringList("paths", paths);
        Assert.That(_backend.GetStringList("paths"), Is.EqualTo(paths));
    }
}
