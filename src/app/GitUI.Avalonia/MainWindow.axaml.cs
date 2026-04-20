using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using GitCommands;
using GitUI.Avalonia.Base;
using GitUI.Avalonia.Dashboard;
using GitUIPluginInterfaces;
using ReactiveUI;

namespace GitUI.Avalonia;

public partial class MainWindow : GitExtensionsWindow
{
    private GitModule? _module;

    public static readonly StyledProperty<bool> HasRepositoryProperty =
        AvaloniaProperty.Register<MainWindow, bool>(nameof(HasRepository));

    public bool HasRepository
    {
        get => GetValue(HasRepositoryProperty);
        private set => SetValue(HasRepositoryProperty, value);
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        BuildMenu();
        LoadRecentRepositories();
    }

    private void LoadRecentRepositories()
    {
        var recent = App.Settings.GetStringList("recentRepositories");
        Dashboard.RecentRepositories = recent.Select(p => new RecentRepo(p)).ToList();
        Dashboard.OnOpenRepository += OpenRepository;
    }

    public void OpenRepository(string path)
    {
        if (!Directory.Exists(path))
        {
            StatusLabel.Text = $"Path not found: {path}";
            return;
        }

        _module = new GitModule(App.GitExecutorProvider, path);
        HasRepository = true;
        Title = $"{Path.GetFileName(path.TrimEnd('/', '\\'))} — Git Extensions";
        StatusLabel.Text = path;
        AddToRecentRepositories(path);
    }

    private void AddToRecentRepositories(string path)
    {
        var recent = App.Settings.GetStringList("recentRepositories").ToList();
        recent.Remove(path);
        recent.Insert(0, path);
        if (recent.Count > 20)
        {
            recent.RemoveRange(20, recent.Count - 20);
        }

        App.Settings.SetStringList("recentRepositories", recent);
        App.Settings.Save();
    }

    private void BuildMenu()
    {
        MainMenu.Items.Add(BuildFileMenu());
        MainMenu.Items.Add(BuildRepositoryMenu());
        MainMenu.Items.Add(BuildCommandsMenu());
        MainMenu.Items.Add(BuildHelpMenu());
    }

    private MenuItem BuildFileMenu()
    {
        var menu = new MenuItem { Header = "_File" };
        menu.Items.Add(new MenuItem { Header = "_Open Repository...", Command = ReactiveCommand.CreateFromTask(OpenRepositoryDialogAsync) });
        menu.Items.Add(new MenuItem { Header = "_Clone Repository..." });
        menu.Items.Add(new MenuItem { Header = "_Init New Repository..." });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "_Settings..." });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem
        {
            Header = "E_xit",
            Command = ReactiveCommand.Create(() => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown()),
        });
        return menu;
    }

    private MenuItem BuildRepositoryMenu()
    {
        var menu = new MenuItem { Header = "_Repository" };
        menu.Items.Add(new MenuItem { Header = "_Fetch" });
        menu.Items.Add(new MenuItem { Header = "_Pull" });
        menu.Items.Add(new MenuItem { Header = "P_ush" });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "Manage _Remotes..." });
        return menu;
    }

    private MenuItem BuildCommandsMenu()
    {
        var menu = new MenuItem { Header = "_Commands" };
        menu.Items.Add(new MenuItem { Header = "_Commit..." });
        menu.Items.Add(new MenuItem { Header = "Create _Branch..." });
        menu.Items.Add(new MenuItem { Header = "Checkout _Branch..." });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "_Stash..." });
        return menu;
    }

    private MenuItem BuildHelpMenu()
    {
        var menu = new MenuItem { Header = "_Help" };
        menu.Items.Add(new MenuItem { Header = "_About Git Extensions" });
        return menu;
    }

    private async System.Threading.Tasks.Task OpenRepositoryDialogAsync()
    {
        var dialog = new OpenRepositoryDialog();
        var path = await dialog.ShowDialog<string?>(this);
        if (path is not null)
        {
            OpenRepository(path);
        }
    }

    private void OnRevisionSelected(GitRevision? revision)
    {
        StatusLabel.Text = revision?.ObjectId.ToShortString() ?? string.Empty;
    }
}
