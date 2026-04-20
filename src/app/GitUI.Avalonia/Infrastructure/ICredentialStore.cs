namespace GitUI.Avalonia.Infrastructure;

public interface ICredentialStore
{
    bool TryGetCredential(string target, out string username, out string password);

    void SaveCredential(string target, string username, string password);

    void DeleteCredential(string target);
}
