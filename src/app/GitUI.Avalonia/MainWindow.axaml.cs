using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using GitCommands;
using GitExtensions.Extensibility;
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
    private ComboBox? _branchSelector;
    private bool _suppressBranchSelection;
    private Action<string>? _leftPanelCheckoutHandler;
    private Action<string>? _leftPanelStatusHandler;
    private Action<string>? _leftPanelErrorHandler;

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
        BuildToolBar();
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
        _ = RefreshBranchSelectorAsync();
        RevisionGrid.SelectedRevisionChanged += OnRevisionSelected;
        DetailsPanel.SetModule(_module);
        LeftPanel.SetModule(_module);

        // Unsubscribe previous handlers (guards against opening a second repo)
        if (_leftPanelCheckoutHandler is not null)
        {
            LeftPanel.CheckoutRequested -= _leftPanelCheckoutHandler;
        }

        if (_leftPanelStatusHandler is not null)
        {
            LeftPanel.StatusRequested -= _leftPanelStatusHandler;
        }

        if (_leftPanelErrorHandler is not null)
        {
            LeftPanel.ErrorOccurred -= _leftPanelErrorHandler;
        }

        _leftPanelCheckoutHandler = branch => _ = CheckoutBranchAsync(branch);
        _leftPanelStatusHandler = msg => StatusLabel.Text = msg;
        _leftPanelErrorHandler = msg => ShowError(msg);

        LeftPanel.CheckoutRequested += _leftPanelCheckoutHandler;
        LeftPanel.StatusRequested += _leftPanelStatusHandler;
        LeftPanel.ErrorOccurred += _leftPanelErrorHandler;
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
        menu.Items.Add(new MenuItem { Header = "_Clone Repository...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new CloneDialog(m))) });
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
        menu.Items.Add(new MenuItem { Header = "Add _Submodule...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new AddSubmoduleDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "_Worktrees...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new ManageWorktreeDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Create _Worktree...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new CreateWorktreeDialog(m))) });
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
        menu.Items.Add(new MenuItem { Header = "_Add Files...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new AddFilesDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Add to .git_ignore...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new AddToGitIgnoreDialog(m))) });
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
        menu.Items.Add(new MenuItem { Header = "Command-_line Help...", Command = ReactiveCommand.CreateFromTask(() => ShowDialogAsync(() => new CommandlineHelpDialog())) });
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

    /// <summary>Displays an error in the status bar. Must be called on the UI thread.</summary>
    public void ShowError(string message)
    {
        StatusLabel.Text = $"Error: {message}";
    }

    private async System.Threading.Tasks.Task FetchAsync()
    {
        if (_module is null)
        {
            return;
        }

        StatusLabel.Text = "Fetching…";
        try
        {
            string output = await _module.GitExecutable.GetOutputAsync("fetch --all");
            StatusLabel.Text = string.IsNullOrWhiteSpace(output) ? "Fetch complete" : output.Trim();
            await RevisionGrid.RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
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

    private void BuildToolBar()
    {
        MainToolBar.Children.Add(MakeToolButton("⊞", "Toggle left panel", ToggleLeftPanel));
        MainToolBar.Children.Add(MakeToolSeparator());
        MainToolBar.Children.Add(MakeToolButton("↻", "Refresh revisions", () => _ = RevisionGrid.RefreshAsync()));
        MainToolBar.Children.Add(MakeToolSeparator());
        MainToolBar.Children.Add(MakeToolButton("Commit…", "Commit staged changes",
            () => _ = ShowModuleDialogAsync(m => new CommitDialog(m))));
        MainToolBar.Children.Add(MakeToolButton("Fetch", "Fetch all remotes", () => _ = FetchAsync()));
        MainToolBar.Children.Add(MakeToolButton("Pull…", "Pull / merge",
            () => _ = ShowModuleDialogAsync(m => new PullDialog(m))));
        MainToolBar.Children.Add(MakeToolButton("Push…", "Push to remote",
            () => _ = ShowModuleDialogAsync(m => new PushDialog(m))));
        MainToolBar.Children.Add(MakeToolButton("Stash…", "Stash local changes",
            () => _ = ShowModuleDialogAsync(m => new StashDialog(m))));
        MainToolBar.Children.Add(MakeToolSeparator());

        _branchSelector = new ComboBox
        {
            Width = 180,
            PlaceholderText = "Branch",
            IsVisible = false,
            Margin = new Thickness(2, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        _branchSelector.SelectionChanged += OnBranchSelectorChanged;
        MainToolBar.Children.Add(_branchSelector);
    }

    private static Button MakeToolButton(string text, string tooltip, Action onClick)
    {
        var btn = new Button
        {
            Content = text,
            Padding = new Thickness(8, 2),
            Margin = new Thickness(1, 0),
        };
        ToolTip.SetTip(btn, tooltip);
        btn.Click += (_, _) => onClick();
        return btn;
    }

    private static Control MakeToolSeparator() =>
        new Border
        {
            Width = 1,
            Height = 20,
            Background = Brushes.Gray,
            Margin = new Thickness(4, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };

    private async System.Threading.Tasks.Task RefreshBranchSelectorAsync()
    {
        if (_module is null || _branchSelector is null)
        {
            return;
        }

        try
        {
            string currentBranch = await System.Threading.Tasks.Task.Run(() => _module.GetCurrentBranchName());
            List<string> branches = await System.Threading.Tasks.Task.Run(
                () => _module.GetRefs(RefsFilter.Heads).Select(r => r.LocalName).OrderBy(n => n).ToList());

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _suppressBranchSelection = true;
                try
                {
                    _branchSelector.ItemsSource = branches;
                    _branchSelector.SelectedItem = string.IsNullOrEmpty(currentBranch) ? null : (object)currentBranch;
                    _branchSelector.IsVisible = true;
                }
                finally
                {
                    _suppressBranchSelection = false;
                }
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => ShowError(ex.Message));
        }
    }

    private void OnBranchSelectorChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressBranchSelection)
        {
            return;
        }

        if (_branchSelector?.SelectedItem is string branch)
        {
            _ = CheckoutBranchAsync(branch);
        }
    }

    private async System.Threading.Tasks.Task CheckoutBranchAsync(string branch)
    {
        if (_module is null)
        {
            return;
        }

        StatusLabel.Text = $"Checking out {branch}…";
        try
        {
            await _module.GitExecutable.GetOutputAsync($"checkout {branch}");
            StatusLabel.Text = $"On branch {branch}";
            await RevisionGrid.RefreshAsync();
            await RefreshBranchSelectorAsync();
            _ = LeftPanel.RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private double _savedLeftPanelWidth = 220;

    private void ToggleLeftPanel()
    {
        var col = RepoView.ColumnDefinitions[0];
        var splitter = RepoView.ColumnDefinitions[1];
        if (col.Width.Value > 0)
        {
            _savedLeftPanelWidth = col.Width.Value;
            col.Width = new GridLength(0);
            splitter.Width = new GridLength(0);
        }
        else
        {
            col.Width = new GridLength(_savedLeftPanelWidth);
            splitter.Width = new GridLength(5);
        }
    }
}
