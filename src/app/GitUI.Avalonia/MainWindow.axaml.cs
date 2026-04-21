using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using GitCommands;
using GitUI.Avalonia.Base;
using GitUI.Avalonia.Dashboard;
using GitUI.Avalonia.Dialogs;
using GitUI.Avalonia.Settings;
using GitUIPluginInterfaces;
using ReactiveUI;
using AvaloniaWindow = Avalonia.Controls.Window;

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

        RevisionGrid.Module = _module;
        RevisionGrid.SelectedRevisionChanged += OnRevisionSelected;
        DetailsPanel.SetModule(_module);
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
        menu.Items.Add(new MenuItem { Header = "_Clone Repository...", Command = ReactiveCommand.CreateFromTask(() => ShowDialogAsync(() => new CloneDialog(_module!))) });
        menu.Items.Add(new MenuItem { Header = "_Init New Repository...", Command = ReactiveCommand.CreateFromTask(() => ShowDialogAsync(() => new InitDialog())) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "_Settings...", Command = ReactiveCommand.Create(OpenSettings) });
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
        menu.Items.Add(new MenuItem { Header = "_Fetch", Command = ReactiveCommand.CreateFromTask(FetchAsync) });
        menu.Items.Add(new MenuItem { Header = "_Pull...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new PullDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "P_ush...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new PushDialog(m))) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "Manage _Remotes...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new RemotesDialog(m))) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "_Resolve Conflicts...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new ResolveConflictsDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "_Clean Repository...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new CleanupRepositoryDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "_Archive...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new ArchiveDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Verify (fsck)...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new VerifyDialog(m))) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "_Submodules...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new SubmodulesDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "_Worktrees...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new ManageWorktreeDialog(m))) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "Compare to _Branch...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new CompareToBranchDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "_Reset Changes...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new ResetChangesDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Sparse Working _Copy...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new SparseWorkingCopyDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Mail_Map...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new MailMapDialog(m))) });
        return menu;
    }

    private MenuItem BuildCommandsMenu()
    {
        var menu = new MenuItem { Header = "_Commands" };
        menu.Items.Add(new MenuItem { Header = "_Commit...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new CommitDialog(m))) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "Create _Branch...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new CreateBranchDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Checkout _Branch...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new CheckoutBranchDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "_Delete Branch...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new DeleteBranchDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Re_name Branch...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new RenameBranchDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "_Merge Branch...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new MergeBranchDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Re_base...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new RebaseDialog(m))) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "Create _Tag...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new CreateTagDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Delete Ta_g...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new DeleteTagDialog(m))) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "_Stash...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new StashDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Cherry _Pick...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new CherryPickDialog(m))) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "Apply _Patch...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new ApplyPatchDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Format Patc_h...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new FormatPatchDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "View Patch...", Command = ReactiveCommand.CreateFromTask(() => new ViewPatchDialog().ShowDialog<object?>(this)) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "_Bisect...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new BisectDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Re_flog...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new ReflogDialog(m))) });
        menu.Items.Add(new MenuItem { Header = ".git_ignore...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new GitIgnoreDialog(m))) });
        menu.Items.Add(new MenuItem { Header = ".git_attributes...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new GitAttributesDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Delete Remote _Branch...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new DeleteRemoteBranchDialog(m))) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "View _Diff...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new DiffDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Git _Log...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new LogDialog(m))) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "_Go to Commit...", Command = ReactiveCommand.CreateFromTask(() => ShowDialogAsync(() => new GoToCommitDialog())) });
        return menu;
    }

    private MenuItem BuildHelpMenu()
    {
        var menu = new MenuItem { Header = "_Help" };
        menu.Items.Add(new MenuItem { Header = "Check for _Updates...", Command = ReactiveCommand.CreateFromTask(() => ShowDialogAsync(() => new UpdatesDialog())) });
        menu.Items.Add(new MenuItem { Header = "_Changelog...", Command = ReactiveCommand.CreateFromTask(() => ShowDialogAsync(() => new ChangeLogDialog())) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "_About Git Extensions", Command = ReactiveCommand.CreateFromTask(() => ShowDialogAsync(() => new AboutDialog())) });
        return menu;
    }

    private async System.Threading.Tasks.Task ShowModuleDialogAsync<T>(Func<GitModule, T> factory)
        where T : AvaloniaWindow
    {
        if (_module is null)
        {
            return;
        }

        await factory(_module).ShowDialog<object?>(this);
    }

    private async System.Threading.Tasks.Task ShowDialogAsync<T>(Func<T> factory)
        where T : AvaloniaWindow
    {
        await factory().ShowDialog<object?>(this);
    }

    private void OpenSettings()
    {
        var settings = new SettingsWindow();
        settings.Show();
    }

    private async System.Threading.Tasks.Task FetchAsync()
    {
        if (_module is null)
        {
            return;
        }

        await System.Threading.Tasks.Task.Run(() => _module.GitExecutable.GetOutput("fetch --all"));
        StatusLabel.Text = "Fetch complete";
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
        _ = DetailsPanel.ShowRevisionAsync(revision);
    }
}
