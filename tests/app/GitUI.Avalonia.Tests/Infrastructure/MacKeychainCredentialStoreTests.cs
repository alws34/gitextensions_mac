using System.Runtime.Versioning;
using GitUI.Avalonia.Infrastructure;
using NUnit.Framework;

namespace GitUI.Avalonia.Tests.Infrastructure;

[TestFixture]
[Platform("MacOsX")]
[SupportedOSPlatform("macos")]
public class MacKeychainCredentialStoreTests
{
    private MacKeychainCredentialStore _store = null!;
    private const string TestTarget = "test.gitextensions.mac-unit-tests";

    [SetUp]
    public void SetUp() => _store = new MacKeychainCredentialStore();

    [TearDown]
    public void TearDown()
    {
        try
        {
            _store.DeleteCredential(TestTarget);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Test]
    public void TryGetCredential_ReturnsFalse_WhenNotStored()
        => Assert.That(_store.TryGetCredential(TestTarget, out _, out _), Is.False);

    [Test]
    public void SaveAndRetrieve_RoundTrip()
    {
        _store.SaveCredential(TestTarget, "testuser", "testpass");
        bool found = _store.TryGetCredential(TestTarget, out string user, out string pass);
        Assert.That(found, Is.True);
        Assert.That(user, Is.EqualTo("testuser"));
        Assert.That(pass, Is.EqualTo("testpass"));
    }

    [Test]
    public void Delete_RemovesCredential()
    {
        _store.SaveCredential(TestTarget, "testuser", "testpass");
        _store.DeleteCredential(TestTarget);
        Assert.That(_store.TryGetCredential(TestTarget, out _, out _), Is.False);
    }
}
