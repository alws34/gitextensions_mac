using Avalonia.Interactivity;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class BuildServerCredentialsDialog : GitExtensionsDialog
{
    public string? Url { get; private set; }
    public string? Username { get; private set; }
    public string? Password { get; private set; }

    public BuildServerCredentialsDialog(string url = "", string username = "")
    {
        InitializeComponent();
        UrlBox.Text = url;
        UsernameBox.Text = username;
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        Url = UrlBox.Text;
        Username = UsernameBox.Text;
        Password = PasswordBox.Text;
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
