using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using GitCommands;
using GitCommands.Logging;
using GitExtensions.Extensibility;
using GitExtUtils;
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
    private Button? _workingDirButton;
    private bool _suppressBranchSelection;
    private Action<string>? _leftPanelCheckoutHandler;
    private Action<string>? _leftPanelOpenRepositoryHandler;
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
        BuildNativeWindowMenu();
        BuildToolBar();
        LoadRecentRepositories();
        var lastRepos = App.Settings.GetStringList("recentRepositories");
        if (App.Settings.GetBool("openLastRepositoryOnStartup", true)
            && lastRepos.Count > 0
            && System.IO.Directory.Exists(lastRepos[0]))
        {
            string autoOpenPath = lastRepos[0];
            Opened += (_, _) => OpenRepository(autoOpenPath);
        }
    }

    private void LoadRecentRepositories()
    {
        var recent = App.Settings.GetStringList("recentRepositories");
        Dashboard.RecentRepositories = recent.Select(p => new RecentRepo(p)).ToList();
        Dashboard.OnOpenRepository += OpenRepository;
        Dashboard.OnClone = () => _ = ShowCloneDialogAsync();
        Dashboard.OnInit = () => _ = ShowDialogAsync(() => new InitDialog());
        Dashboard.OnBrowse = () => _ = OpenRepositoryDialogAsync();
    }

    private async System.Threading.Tasks.Task ShowCloneDialogAsync()
    {
        // CloneDialog requires a GitModule for git execution; use the current module
        // or create a temporary one rooted at the user's home directory.
        var module = _module ?? new GitModule(App.GitExecutorProvider,
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile));
        await new CloneDialog(module).ShowDialog<object?>(this);
    }

    private void CloseRepository()
    {
        _module = null;
        HasRepository = false;
        Title = "Git Extensions";
        StatusLabel.Text = string.Empty;
        BranchLabel.Text = string.Empty;
        AheadBehindLabel.IsVisible = false;
        StagedCountLabel.IsVisible = false;
        UnstagedCountLabel.IsVisible = false;
        ActionBar.IsVisible = false;
        if (_workingDirButton is not null)
        {
            _workingDirButton.Content = "(no repo)";
        }

        if (_branchSelector is not null)
        {
            _branchSelector.ItemsSource = null;
            _branchSelector.IsVisible = false;
        }

        Dashboard.Refresh();
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
        string repoName = Path.GetFileName(path.TrimEnd('/', '\\'));
        Title = $"{repoName} — Git Extensions";
        StatusLabel.Text = string.Empty;
        BranchLabel.Text = "⎇  …";
        if (_workingDirButton is not null)
        {
            _workingDirButton.Content = repoName;
        }

        AddToRecentRepositories(path);

        RevisionGrid.Module = _module;
        FilterBar.FilterChanged += (text, type) => _ = RevisionGrid.SetFilterAsync(text, type);
        _ = RefreshBranchSelectorAsync();
        _ = RefreshActionBarsAsync();
        _ = RefreshStatusBarCountsAsync();
        RevisionGrid.SelectedRevisionChanged += OnRevisionSelected;
        RevisionGrid.CherryPickHashRequested += hash =>
            _ = ShowModuleDialogAsync(m => new CherryPickDialog(m, hash));
        RevisionGrid.RevertHashRequested += hash =>
            _ = ShowModuleDialogAsync(m => new RevertCommitDialog(m, hash));
        RevisionGrid.CheckoutHashRequested += hash => _ = CheckoutHashAsync(hash);
        RevisionGrid.CheckoutBranchRequested += branch => _ = CheckoutBranchAsync(branch);
        RevisionGrid.CheckoutRemoteBranchRequested += branch => _ = CheckoutRemoteBranchAsync(branch);
        RevisionGrid.CreateBranchAtHashRequested += ignored =>
            _ = ShowModuleDialogAsync(m => new CreateBranchDialog(m));
        RevisionGrid.CreateTagAtHashRequested += ignored =>
            _ = ShowModuleDialogAsync(m => new CreateTagDialog(m));
        RevisionGrid.ResetHardToHashRequested += hash => _ = ResetHardAsync(hash);
        RevisionGrid.InteractiveRebaseRequested += hash =>
            _ = ShowModuleDialogAsync(m => new InteractiveRebaseDialog(m, hash));
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

        if (_leftPanelOpenRepositoryHandler is not null)
        {
            LeftPanel.OpenRepositoryRequested -= _leftPanelOpenRepositoryHandler;
        }

        if (_leftPanelErrorHandler is not null)
        {
            LeftPanel.ErrorOccurred -= _leftPanelErrorHandler;
        }

        _leftPanelCheckoutHandler = branch => _ = CheckoutBranchAsync(branch);
        _leftPanelOpenRepositoryHandler = OpenRepository;
        _leftPanelStatusHandler = msg => StatusLabel.Text = msg;
        _leftPanelErrorHandler = msg => ShowError(msg);

        LeftPanel.CheckoutRequested += _leftPanelCheckoutHandler;
        LeftPanel.OpenRepositoryRequested += _leftPanelOpenRepositoryHandler;
        LeftPanel.StatusRequested += _leftPanelStatusHandler;
        LeftPanel.ErrorOccurred += _leftPanelErrorHandler;
    }

    private void AddToRecentRepositories(string path)
    {
        var recent = App.Settings.GetStringList("recentRepositories").ToList();
        recent.Remove(path);
        recent.Insert(0, path);
        int maxRecentRepositories = Math.Clamp(App.Settings.GetInt("recentRepositoriesLimit", 20), 1, 50);
        if (recent.Count > maxRecentRepositories)
        {
            recent.RemoveRange(maxRecentRepositories, recent.Count - maxRecentRepositories);
        }

        App.Settings.SetStringList("recentRepositories", recent);
        App.Settings.Save();
    }

    private void BuildMenu()
    {
        MainMenu.Items.Add(BuildFileMenu());
        MainMenu.Items.Add(BuildDashboardMenu());
        MainMenu.Items.Add(BuildRepositoryMenu());
        MainMenu.Items.Add(BuildCommandsMenu());
        MainMenu.Items.Add(BuildToolsMenu());
        MainMenu.Items.Add(BuildViewMenu());
        MainMenu.Items.Add(BuildNavigateMenu());
        MainMenu.Items.Add(BuildHelpMenu());
    }

    private void BuildNativeWindowMenu()
    {
        if (!OperatingSystem.IsMacOS() || !App.Settings.GetBool("useNativeMenu", true))
        {
            return;
        }

        var nativeMenu = new NativeMenu();
        foreach (object? item in MainMenu.Items)
        {
            if (ToNativeMenuItem(item) is { } nativeItem)
            {
                nativeMenu.Items.Add(nativeItem);
            }
        }

        NativeMenu.SetMenu(this, nativeMenu);
        MainMenu.IsVisible = false;
    }

    private static NativeMenuItemBase? ToNativeMenuItem(object? source)
    {
        if (source is Separator)
        {
            return new NativeMenuItemSeparator();
        }

        if (source is not MenuItem menuItem)
        {
            return null;
        }

        var nativeItem = new NativeMenuItem
        {
            Header = NormalizeMenuHeader(menuItem.Header),
            Gesture = ToMacGesture(menuItem.InputGesture),
            CommandParameter = menuItem.CommandParameter,
            IsEnabled = menuItem.IsEnabled,
            IsVisible = menuItem.IsVisible,
        };

        bool isCheckItem = menuItem.Icon is not null
            || menuItem.IsChecked
            || nativeItem.Header.StartsWith("Show ", StringComparison.Ordinal);
        if (isCheckItem)
        {
            nativeItem.ToggleType = NativeMenuItemToggleType.CheckBox;
            nativeItem.IsChecked = menuItem.Icon is not null || menuItem.IsChecked;
            nativeItem.Command = ReactiveCommand.Create(() =>
            {
                if (menuItem.Command?.CanExecute(menuItem.CommandParameter) == true)
                {
                    menuItem.Command.Execute(menuItem.CommandParameter);
                }

                nativeItem.IsChecked = menuItem.Icon is not null || menuItem.IsChecked;
            });
        }
        else
        {
            nativeItem.Command = menuItem.Command;
        }

        NativeMenu? subMenu = null;
        foreach (object? child in menuItem.Items)
        {
            if (ToNativeMenuItem(child) is { } nativeChild)
            {
                subMenu ??= new NativeMenu();
                subMenu.Items.Add(nativeChild);
            }
        }

        if (subMenu is not null)
        {
            nativeItem.Menu = subMenu;
        }

        return nativeItem;
    }

    private static string NormalizeMenuHeader(object? header)
        => (header?.ToString() ?? string.Empty).Replace("_", string.Empty, StringComparison.Ordinal).Replace("...", "…", StringComparison.Ordinal);

    private static KeyGesture? ToMacGesture(KeyGesture? gesture)
    {
        if (gesture is null)
        {
            return null;
        }

        KeyModifiers modifiers = gesture.KeyModifiers;
        if ((modifiers & KeyModifiers.Control) != 0)
        {
            modifiers &= ~KeyModifiers.Control;
            modifiers |= KeyModifiers.Meta;
        }

        return new KeyGesture(gesture.Key, modifiers);
    }

    private MenuItem BuildRecentReposMenu()
    {
        var sub = new MenuItem { Header = "Recent _Repositories" };
        var recent = App.Settings.GetStringList("recentRepositories");

        if (recent.Count == 0)
        {
            sub.Items.Add(new MenuItem { Header = "(none)", IsEnabled = false });
            return sub;
        }

        foreach (string path in recent)
        {
            string capturedPath = path;
            sub.Items.Add(new MenuItem
            {
                Header = capturedPath,
                Command = ReactiveCommand.Create(() => OpenRepository(capturedPath)),
            });
        }

        return sub;
    }

    private MenuItem BuildFileMenu()
    {
        var menu = new MenuItem { Header = "_File" };
        menu.Items.Add(new MenuItem { Header = "_Open Repository...", InputGesture = new KeyGesture(Key.O, KeyModifiers.Control), Command = ReactiveCommand.CreateFromTask(OpenRepositoryDialogAsync) });
        menu.Items.Add(new MenuItem { Header = "_Clone Repository...", Command = ReactiveCommand.CreateFromTask(ShowCloneDialogAsync) });
        menu.Items.Add(new MenuItem { Header = "_Init New Repository...", Command = ReactiveCommand.CreateFromTask(() => ShowDialogAsync(() => new InitDialog())) });
        menu.Items.Add(BuildRecentReposMenu());
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "_Settings...", InputGesture = new KeyGesture(Key.OemComma, KeyModifiers.Control), Command = ReactiveCommand.Create(OpenSettings) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "_Close Repository (Go to Dashboard)", Command = ReactiveCommand.Create(ShowDashboard) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem
        {
            Header = "E_xit",
            Command = ReactiveCommand.Create(() => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown()),
        });
        return menu;
    }

    private MenuItem BuildDashboardMenu()
    {
        var menu = new MenuItem { Header = "_Dashboard" };
        menu.Items.Add(new MenuItem { Header = "_Go to Dashboard", Command = ReactiveCommand.Create(ShowDashboard) });
        menu.Items.Add(new MenuItem { Header = "_Refresh Dashboard", Command = ReactiveCommand.Create(RefreshDashboard) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "_Open Repository...", Command = ReactiveCommand.CreateFromTask(OpenRepositoryDialogAsync) });
        menu.Items.Add(new MenuItem { Header = "_Clone Repository...", Command = ReactiveCommand.CreateFromTask(ShowCloneDialogAsync) });
        menu.Items.Add(new MenuItem { Header = "_Create New Repository...", Command = ReactiveCommand.CreateFromTask(() => ShowDialogAsync(() => new InitDialog())) });
        menu.Items.Add(BuildRecentReposMenu());
        return menu;
    }

    private MenuItem BuildRepositoryMenu()
    {
        var menu = new MenuItem { Header = "_Repository" };
        menu.Items.Add(new MenuItem { Header = "_Status...", Command = ReactiveCommand.CreateFromTask(ShowRepositoryStatusAsync) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "_Fetch", Command = ReactiveCommand.CreateFromTask(FetchAsync) });
        menu.Items.Add(new MenuItem { Header = "Fetch and P_rune", Command = ReactiveCommand.CreateFromTask(FetchPruneAsync) });
        menu.Items.Add(new MenuItem { Header = "_Pull...", Command = ReactiveCommand.CreateFromTask(ShowPullDialogAsync) });
        menu.Items.Add(new MenuItem { Header = "P_ush...", Command = ReactiveCommand.CreateFromTask(ShowPushDialogAsync) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "Manage _Remotes...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new RemotesDialog(m))) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "_Resolve Conflicts...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new ResolveConflictsDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "_Clean Repository...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new CleanupRepositoryDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "_Archive...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new ArchiveDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Verify (fsck)...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new VerifyDialog(m))) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "Manage _Submodules...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new SubmodulesDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "_Update All Submodules", Command = ReactiveCommand.CreateFromTask(UpdateAllSubmodulesAsync) });
        menu.Items.Add(new MenuItem { Header = "Synchronize All Su_bmodules", Command = ReactiveCommand.CreateFromTask(SynchronizeAllSubmodulesAsync) });
        menu.Items.Add(new MenuItem { Header = "Add _Submodule...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new AddSubmoduleDialog(m))) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "Manage _Worktrees...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new ManageWorktreeDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Create _Worktree...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new CreateWorktreeDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Prune Wor_ktrees", Command = ReactiveCommand.CreateFromTask(PruneWorktreesAsync) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "Compare to _Branch...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new CompareToBranchDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "_Reset Changes...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new ResetChangesDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Sparse Working _Copy...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new SparseWorkingCopyDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Mail_Map...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new MailMapDialog(m))) });
        menu.Items.Add(new Separator());
        menu.Items.Add(BuildMaintenanceMenu());
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem
        {
            Header = "_Refresh",
            InputGesture = new KeyGesture(Key.F5),
            Command = ReactiveCommand.CreateFromTask(RefreshRepositoryAsync),
        });
        return menu;
    }

    private MenuItem BuildMaintenanceMenu()
    {
        var menu = new MenuItem { Header = "Git _Maintenance" };
        menu.Items.Add(new MenuItem { Header = "_Compress Git Database", Command = ReactiveCommand.CreateFromTask(CompressGitDatabaseAsync) });
        menu.Items.Add(new MenuItem { Header = "_Prune Unreachable Objects", Command = ReactiveCommand.CreateFromTask(PruneUnreachableObjectsAsync) });
        menu.Items.Add(new MenuItem { Header = "Prune _Worktrees", Command = ReactiveCommand.CreateFromTask(PruneWorktreesAsync) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "_Recover Lost Objects...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new VerifyDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "_Delete index.lock", Command = ReactiveCommand.CreateFromTask(DeleteIndexLockAsync) });
        menu.Items.Add(new MenuItem { Header = "Edit Local Git _Config...", Command = ReactiveCommand.CreateFromTask(EditLocalGitConfigAsync) });
        return menu;
    }

    private MenuItem BuildCommandsMenu()
    {
        var menu = new MenuItem { Header = "_Commands" };
        menu.Items.Add(new MenuItem { Header = "_Commit...", InputGesture = new KeyGesture(Key.Enter, KeyModifiers.Control), Command = ReactiveCommand.CreateFromTask(ShowCommitDialogAsync) });
        menu.Items.Add(new MenuItem { Header = "_Add Files...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new AddFilesDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Add to .git_ignore...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new AddToGitIgnoreDialog(m))) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "Create _Branch...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new CreateBranchDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Checkout _Branch...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new CheckoutBranchDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "_Delete Branch...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new DeleteBranchDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Re_name Branch...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new RenameBranchDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "_Merge Branch...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new MergeBranchDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Re_base...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new RebaseDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "_Interactive Rebase…", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new InteractiveRebaseDialog(m, "HEAD~1"))) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "Create _Tag...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new CreateTagDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Delete Ta_g...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new DeleteTagDialog(m))) });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem { Header = "_Stash...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new StashDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Cherry _Pick...", Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new CherryPickDialog(m))) });
        menu.Items.Add(new MenuItem { Header = "Re_vert Commit...", Command = ReactiveCommand.CreateFromTask(RevertSelectedCommitAsync) });
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
        menu.Items.Add(new MenuItem { Header = "_Go to Commit...", InputGesture = new KeyGesture(Key.G, KeyModifiers.Control), Command = ReactiveCommand.CreateFromTask(GoToCommitAsync) });
        return menu;
    }

    private MenuItem BuildToolsMenu()
    {
        var menu = new MenuItem { Header = "_Tools" };
        menu.Items.Add(new MenuItem
        {
            Header = "Open _Terminal Here",
            Command = ReactiveCommand.Create(() =>
            {
                if (_module is not null)
                {
                    var psi = new System.Diagnostics.ProcessStartInfo { FileName = "open", UseShellExecute = false };
                    psi.ArgumentList.Add("-a");
                    psi.ArgumentList.Add("Terminal");
                    psi.ArgumentList.Add(_module.WorkingDir);
                    System.Diagnostics.Process.Start(psi);
                }
            }),
        });
        menu.Items.Add(new MenuItem
        {
            Header = "Open in _Finder",
            Command = ReactiveCommand.Create(() =>
            {
                if (_module is not null)
                {
                    System.Diagnostics.Process.Start("open", _module.WorkingDir);
                }
            }),
        });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem
        {
            Header = "_Scripts…",
            Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new Dialogs.ScriptsManagerDialog(m))),
        });
        menu.Items.Add(new MenuItem
        {
            Header = "Git _Hooks…",
            Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new Dialogs.HooksDialog(m))),
        });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem
        {
            Header = "Git _Command Log",
            InputGesture = new KeyGesture(Key.F12),
            Command = ReactiveCommand.CreateFromTask(ShowGitCommandLogAsync),
        });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem
        {
            Header = "_Settings…",
            Command = ReactiveCommand.Create(OpenSettings),
        });
        return menu;
    }

    private MenuItem BuildViewMenu()
    {
        var menu = new MenuItem { Header = "_View" };
        menu.Items.Add(new MenuItem
        {
            Header = "Toggle _Left Panel",
            Command = ReactiveCommand.Create(ToggleLeftPanel),
        });
        menu.Items.Add(new Separator());
        menu.Items.Add(MakeCheckMenuItem("Show _Stashes in Graph", "showStashesInGraph"));
        menu.Items.Add(MakeCheckMenuItem("Show _Worktrees in Graph", "showWorktreesInGraph"));
        menu.Items.Add(MakeCheckMenuItem("Show _Tags", "showTags"));
        menu.Items.Add(MakeCheckMenuItem("Show _First Parent Only", "showFirstParentOnly"));
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem
        {
            Header = "_Refresh",
            InputGesture = new KeyGesture(Key.F5),
            Command = ReactiveCommand.CreateFromTask(() => RevisionGrid.RefreshAsync()),
        });
        return menu;
    }

    private static MenuItem MakeCheckMenuItem(string header, string settingKey)
    {
        bool current = App.Settings.GetBool(settingKey, false);
        var item = new MenuItem
        {
            Header = header,
            Icon = current ? new TextBlock { Text = "✓", FontSize = 12 } : null,
        };
        item.Command = ReactiveCommand.Create(() =>
        {
            bool val = !App.Settings.GetBool(settingKey, false);
            App.Settings.SetBool(settingKey, val);
            App.Settings.Save();
            item.Icon = val ? new TextBlock { Text = "✓", FontSize = 12 } : null;
        });
        return item;
    }

    private MenuItem BuildNavigateMenu()
    {
        var menu = new MenuItem { Header = "_Navigate" };
        menu.Items.Add(new MenuItem
        {
            Header = "Go to _Parent Commit",
            InputGesture = new KeyGesture(Key.Up, KeyModifiers.Alt),
            Command = ReactiveCommand.Create(() => RevisionGrid.NavigateParent()),
        });
        menu.Items.Add(new MenuItem
        {
            Header = "Go to _Child Commit",
            InputGesture = new KeyGesture(Key.Down, KeyModifiers.Alt),
            Command = ReactiveCommand.Create(() => RevisionGrid.NavigateChild()),
        });
        menu.Items.Add(new MenuItem
        {
            Header = "_Back",
            InputGesture = new KeyGesture(Key.Left, KeyModifiers.Alt),
            Command = ReactiveCommand.Create(() => RevisionGrid.NavigateBack()),
        });
        menu.Items.Add(new MenuItem
        {
            Header = "_Forward",
            InputGesture = new KeyGesture(Key.Right, KeyModifiers.Alt),
            Command = ReactiveCommand.Create(() => RevisionGrid.NavigateForward()),
        });
        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem
        {
            Header = "_Go to Commit…",
            InputGesture = new KeyGesture(Key.G, KeyModifiers.Control),
            Command = ReactiveCommand.CreateFromTask(GoToCommitAsync),
        });
        return menu;
    }

    private async System.Threading.Tasks.Task GoToCommitAsync()
    {
        var dialog = new GoToCommitDialog();
        var result = await dialog.ShowDialog<string?>(this);
        if (result is { Length: >= 4 })
        {
            RevisionGrid.ScrollToHash(result);
        }
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

    private async System.Threading.Tasks.Task ShowCommitDialogAsync()
    {
        if (_module is null)
        {
            return;
        }

        var dialog = new CommitDialog(_module);
        dialog.CommitMade += () =>
        {
            _ = Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await RevisionGrid.RefreshAsync();
                await RefreshBranchSelectorAsync();
                await RefreshStatusBarCountsAsync();
            });
        };
        await dialog.ShowDialog<object?>(this);
    }

    private async System.Threading.Tasks.Task RevertSelectedCommitAsync()
    {
        if (_module is null)
        {
            return;
        }

        string hash = "HEAD";
        await ShowModuleDialogAsync(m => new RevertCommitDialog(m, hash));
    }

    private async System.Threading.Tasks.Task ShowPushDialogAsync()
    {
        if (_module is null)
        {
            return;
        }

        var dialog = new PushDialog(_module);
        bool? result = await dialog.ShowDialog<bool?>(this);
        if (result == true)
        {
            await RevisionGrid.RefreshAsync();
            await RefreshBranchSelectorAsync();
            await RefreshStatusBarCountsAsync();
        }
    }

    private async System.Threading.Tasks.Task ShowPullDialogAsync()
    {
        if (_module is null)
        {
            return;
        }

        var dialog = new PullDialog(_module);
        bool? result = await dialog.ShowDialog<bool?>(this);
        if (result == true)
        {
            await RevisionGrid.RefreshAsync();
            await RefreshBranchSelectorAsync();
            await RefreshStatusBarCountsAsync();
            _ = LeftPanel.RefreshAsync();
        }
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

    private void ShowDashboard()
    {
        CloseRepository();
    }

    private void RefreshDashboard()
    {
        Dashboard.Refresh();
        if (!HasRepository)
        {
            StatusLabel.Text = string.Empty;
        }
    }

    private void OpenSettings()
    {
        var settings = new SettingsWindow();
        settings.Show();
    }

    /// <summary>Called from the macOS native menu bar "Settings…" item.</summary>
    public void OpenSettingsFromNativeMenu() => OpenSettings();

    /// <summary>Displays an error in the status bar. Must be called on the UI thread.</summary>
    public void ShowError(string message)
    {
        StatusLabel.Text = $"Error: {message}";
    }

    private async System.Threading.Tasks.Task RefreshRepositoryAsync()
    {
        await RevisionGrid.RefreshAsync();
        await RefreshStatusBarCountsAsync();
    }

    private async System.Threading.Tasks.Task ShowRepositoryStatusAsync()
    {
        if (_module is null)
        {
            return;
        }

        var capturedModule = _module;
        var dialog = new StatusDialog("Repository Status", async progress =>
        {
            string output = await capturedModule.GitExecutable.GetOutputAsync("status --short --branch");
            progress.Report(string.IsNullOrWhiteSpace(output) ? "Working tree clean." : output.TrimEnd());
        });
        await dialog.ShowDialog<object?>(this);
    }

    private async System.Threading.Tasks.Task ShowGitCommandLogAsync()
    {
        var dialog = new StatusDialog("Git Command Log", progress =>
        {
            CommandLogEntry[] entries = [.. CommandLog.Commands];
            if (entries.Length == 0)
            {
                progress.Report("No commands logged yet.");
                return System.Threading.Tasks.Task.CompletedTask;
            }

            foreach (CommandLogEntry entry in entries)
            {
                progress.Report(entry.ColumnLine);
            }

            return System.Threading.Tasks.Task.CompletedTask;
        });
        await dialog.ShowDialog<object?>(this);
    }

    private async System.Threading.Tasks.Task FetchAsync()
    {
        if (_module is null)
        {
            return;
        }

        var capturedModule = _module;
        var dialog = new GitProgressDialog(
            "Fetching all remotes…",
            () => capturedModule.GitExecutable.GetOutputAsync("fetch --all"));
        await dialog.ShowDialog<object?>(this);
        await RevisionGrid.RefreshAsync();
        await RefreshActionBarsAsync();
        await RefreshStatusBarCountsAsync();
    }

    private async System.Threading.Tasks.Task UpdateAllSubmodulesAsync()
    {
        await RunRepositoryCommandAsync("Updating all submodules...", "submodule update --init --recursive", refreshLeftPanel: true);
    }

    private async System.Threading.Tasks.Task SynchronizeAllSubmodulesAsync()
    {
        await RunRepositoryCommandAsync("Synchronizing all submodules...", "submodule sync", refreshLeftPanel: true);
    }

    private async System.Threading.Tasks.Task PruneWorktreesAsync()
    {
        await RunRepositoryCommandAsync("Pruning worktrees...", "worktree prune", refreshLeftPanel: true);
    }

    private async System.Threading.Tasks.Task CompressGitDatabaseAsync()
    {
        await RunRepositoryCommandAsync("Compressing git database...", "gc");
    }

    private async System.Threading.Tasks.Task PruneUnreachableObjectsAsync()
    {
        await RunRepositoryCommandAsync("Pruning unreachable objects...", "prune");
    }

    private async System.Threading.Tasks.Task DeleteIndexLockAsync()
    {
        if (_module is null)
        {
            return;
        }

        var capturedModule = _module;
        try
        {
            await System.Threading.Tasks.Task.Run(() => capturedModule.UnlockIndex(includeSubmodules: true));
            StatusLabel.Text = "Deleted index.lock.";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async System.Threading.Tasks.Task EditLocalGitConfigAsync()
    {
        if (_module is null)
        {
            return;
        }

        string fileName = _module.ResolveGitInternalPath("config");
        string content = File.Exists(fileName) ? File.ReadAllText(fileName) : string.Empty;
        await new EditorDialog("Local Git Config", content, fileName).ShowDialog<object?>(this);
    }

    private async System.Threading.Tasks.Task RunRepositoryCommandAsync(string title, string arguments, bool refreshBranches = false, bool refreshLeftPanel = false)
    {
        if (_module is null)
        {
            return;
        }

        var capturedModule = _module;
        var dialog = new GitProgressDialog(
            title,
            () => capturedModule.GitExecutable.GetOutputAsync(arguments));
        await dialog.ShowDialog<object?>(this);
        await RevisionGrid.RefreshAsync();
        if (refreshBranches)
        {
            await RefreshBranchSelectorAsync();
        }

        await RefreshActionBarsAsync();
        await RefreshStatusBarCountsAsync();
        if (refreshLeftPanel)
        {
            _ = LeftPanel.RefreshAsync();
        }
    }

    private string? _activeAbortCommand;

    private void ActionBar_Dismiss(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        ActionBar.IsVisible = false;
    }

    private void ActionBar_Abort(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_module is null || _activeAbortCommand is null)
        {
            return;
        }

        string cmd = _activeAbortCommand;
        var capturedModule = _module;
        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            capturedModule.GitExecutable.GetOutput(cmd);
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                ActionBar.IsVisible = false;
                await RevisionGrid.RefreshAsync();
                await RefreshActionBarsAsync();
                await RefreshStatusBarCountsAsync();
            });
        });
    }

    private async System.Threading.Tasks.Task RefreshActionBarsAsync()
    {
        if (_module is null)
        {
            return;
        }

        var capturedModule = _module;
        var (merge, cherry, revert, bisect) = await System.Threading.Tasks.Task.Run(() =>
        {
            bool m = capturedModule.InTheMiddleOfMerge();
            string gitDir = capturedModule.WorkingDirGitDir;
            bool c = File.Exists(Path.Combine(gitDir, "CHERRY_PICK_HEAD"));
            bool rv = File.Exists(Path.Combine(gitDir, "REVERT_HEAD"));
            bool b = capturedModule.InTheMiddleOfBisect();
            return (m, c, rv, b);
        });

        string gitDir = capturedModule.WorkingDirGitDir;
        string? msg = merge ? $"MERGE_HEAD exists in {gitDir} — resolve conflicts and commit, or abort." :
                      cherry ? "Cherry-pick in progress — resolve conflicts and commit, or abort." :
                      revert ? "Revert in progress — resolve conflicts and commit, or abort." :
                      bisect ? "Bisect in progress — mark commits as good or bad, or reset." :
                      null;

        string? abortCmd = merge ? "merge --abort" :
                           cherry ? "cherry-pick --abort" :
                           revert ? "revert --abort" :
                           bisect ? "bisect reset" :
                           null;

        string? abortLabel = merge ? "Abort Merge" :
                             cherry ? "Abort Cherry-pick" :
                             revert ? "Abort Revert" :
                             bisect ? "Reset Bisect" :
                             null;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _activeAbortCommand = abortCmd;
            ActionBarText.Text = msg;
            ActionBar_AbortBtn.Content = abortLabel;
            ActionBar.IsVisible = msg is not null;
        });
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
        // Toggle left panel
        MainToolBar.Children.Add(MakeToolButton("avares://GitUI.Avalonia/Assets/Icons/LayoutSidebarTopLeft.png", "⊟", "Toggle left panel (Ctrl+K)", ToggleLeftPanel));
        MainToolBar.Children.Add(MakeToolSeparator());

        // Dashboard
        MainToolBar.Children.Add(MakeToolButton("⌂", "Dashboard (close repository)", ShowDashboard));
        MainToolBar.Children.Add(MakeToolSeparator());

        // Refresh
        MainToolBar.Children.Add(MakeToolButton("avares://GitUI.Avalonia/Assets/Icons/ReloadRevisions.png", "↺", "Refresh (F5)", () => _ = RevisionGrid.RefreshAsync()));
        MainToolBar.Children.Add(MakeToolSeparator());

        // Working directory button
        _workingDirButton = new Button
        {
            Content = "(no repo)",
            Classes = { "ToolBtn" },
            Padding = new Thickness(6, 2),
            VerticalContentAlignment = VerticalAlignment.Center,
            FontSize = 12,
        };
        ToolTip.SetTip(_workingDirButton, "Working directory — click to switch repository");
        _workingDirButton.Click += (_, _) => ShowRecentReposPopup(_workingDirButton);
        MainToolBar.Children.Add(_workingDirButton);
        MainToolBar.Children.Add(MakeToolSeparator());

        // Branch selector
        _branchSelector = new ComboBox
        {
            MinWidth = 120,
            MaxWidth = 200,
            PlaceholderText = "Branch",
            IsVisible = false,
            Margin = new Thickness(4, 0),
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 12,
        };
        _branchSelector.SelectionChanged += OnBranchSelectorChanged;
        MainToolBar.Children.Add(_branchSelector);
        MainToolBar.Children.Add(MakeToolSeparator());

        // Submodules and worktrees
        MainToolBar.Children.Add(MakeSubmodulesSplitButton());
        MainToolBar.Children.Add(MakeWorktreesSplitButton());
        MainToolBar.Children.Add(MakeToolSeparator());

        // Commit
        MainToolBar.Children.Add(MakeToolButton("avares://GitUI.Avalonia/Assets/Icons/RepoStateClean.png", "✎", "Commit staged changes (Ctrl+Enter)",
            () => _ = ShowCommitDialogAsync()));
        MainToolBar.Children.Add(MakeToolSeparator());

        // Pull split button
        MainToolBar.Children.Add(MakePullSplitButton());

        // Push
        MainToolBar.Children.Add(MakeToolButton("avares://GitUI.Avalonia/Assets/Icons/Push.png", "⬆", "Push to remote",
            () => _ = ShowPushDialogAsync()));
        MainToolBar.Children.Add(MakeToolSeparator());

        // Stash split button
        MainToolBar.Children.Add(MakeStashSplitButton());
        MainToolBar.Children.Add(MakeToolSeparator());

        // File Explorer (Finder)
        MainToolBar.Children.Add(MakeToolButton("📁", "Open in Finder", () =>
        {
            if (_module is not null)
            {
                System.Diagnostics.Process.Start("open", _module.WorkingDir);
            }
        }));

        // Terminal
        MainToolBar.Children.Add(MakeToolButton("⌨", "Open Terminal here", () =>
        {
            if (_module is not null)
            {
                var psi = new System.Diagnostics.ProcessStartInfo { FileName = "open", UseShellExecute = false };
                psi.ArgumentList.Add("-a");
                psi.ArgumentList.Add("Terminal");
                psi.ArgumentList.Add(_module.WorkingDir);
                System.Diagnostics.Process.Start(psi);
            }
        }));

        // Settings
        MainToolBar.Children.Add(MakeToolButton("⚙", "Settings", OpenSettings));
    }

    private SplitButton MakePullSplitButton()
    {
        Control pullContent = MakeToolIcon("avares://GitUI.Avalonia/Assets/Icons/PullMerge.png") ?? (Control)new TextBlock { Text = "⬇" };
        var flyout = new MenuFlyout();
        flyout.Items.Add(new MenuItem
        {
            Header = "Pull + Merge",
            Command = ReactiveCommand.CreateFromTask(ShowPullDialogAsync),
        });
        flyout.Items.Add(new MenuItem
        {
            Header = "Pull + Rebase",
            Command = ReactiveCommand.CreateFromTask(PullRebaseAsync),
        });
        flyout.Items.Add(new MenuItem
        {
            Header = "Fetch All",
            Command = ReactiveCommand.CreateFromTask(FetchAsync),
        });
        flyout.Items.Add(new MenuItem
        {
            Header = "Fetch (pruning)",
            Command = ReactiveCommand.CreateFromTask(FetchPruneAsync),
        });

        var btn = new SplitButton
        {
            Content = pullContent,
            Flyout = flyout,
            Classes = { "ToolBtn" },
            Padding = new Thickness(4, 2),
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        ToolTip.SetTip(btn, "Pull / merge (dropdown for more options)");
        btn.Click += (_, _) => _ = ShowPullDialogAsync();
        return btn;
    }

    private SplitButton MakeSubmodulesSplitButton()
    {
        var flyout = new MenuFlyout();
        flyout.Items.Add(new MenuItem
        {
            Header = "Manage Submodules...",
            Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new SubmodulesDialog(m))),
        });
        flyout.Items.Add(new MenuItem
        {
            Header = "Update All Submodules",
            Command = ReactiveCommand.CreateFromTask(UpdateAllSubmodulesAsync),
        });
        flyout.Items.Add(new MenuItem
        {
            Header = "Synchronize All Submodules",
            Command = ReactiveCommand.CreateFromTask(SynchronizeAllSubmodulesAsync),
        });
        flyout.Items.Add(new MenuItem
        {
            Header = "Add Submodule...",
            Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new AddSubmoduleDialog(m))),
        });

        var btn = new SplitButton
        {
            Content = new TextBlock { Text = "Sub", FontSize = 11 },
            Flyout = flyout,
            Classes = { "ToolBtn" },
            Padding = new Thickness(4, 2),
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        ToolTip.SetTip(btn, "Submodules");
        btn.Click += (_, _) => _ = ShowModuleDialogAsync(m => new SubmodulesDialog(m));
        return btn;
    }

    private SplitButton MakeWorktreesSplitButton()
    {
        var flyout = new MenuFlyout();
        flyout.Items.Add(new MenuItem
        {
            Header = "Manage Worktrees...",
            Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new ManageWorktreeDialog(m))),
        });
        flyout.Items.Add(new MenuItem
        {
            Header = "Create Worktree...",
            Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new CreateWorktreeDialog(m))),
        });
        flyout.Items.Add(new MenuItem
        {
            Header = "Prune Worktrees",
            Command = ReactiveCommand.CreateFromTask(PruneWorktreesAsync),
        });

        var btn = new SplitButton
        {
            Content = new TextBlock { Text = "WT", FontSize = 11 },
            Flyout = flyout,
            Classes = { "ToolBtn" },
            Padding = new Thickness(4, 2),
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        ToolTip.SetTip(btn, "Worktrees");
        btn.Click += (_, _) => _ = ShowModuleDialogAsync(m => new ManageWorktreeDialog(m));
        return btn;
    }

    private SplitButton MakeStashSplitButton()
    {
        Control stashContent = MakeToolIcon("avares://GitUI.Avalonia/Assets/Icons/stash.png") ?? (Control)new TextBlock { Text = "≡" };
        var flyout = new MenuFlyout();
        flyout.Items.Add(new MenuItem
        {
            Header = "Stash",
            Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new StashDialog(m))),
        });
        flyout.Items.Add(new MenuItem
        {
            Header = "Stash Staged",
            Command = ReactiveCommand.CreateFromTask(StashStagedAsync),
        });
        flyout.Items.Add(new MenuItem
        {
            Header = "Pop Stash",
            Command = ReactiveCommand.CreateFromTask(StashPopAsync),
        });
        flyout.Items.Add(new MenuItem
        {
            Header = "Manage Stashes…",
            Command = ReactiveCommand.CreateFromTask(() => ShowModuleDialogAsync(m => new StashDialog(m))),
        });

        var btn = new SplitButton
        {
            Content = stashContent,
            Flyout = flyout,
            Classes = { "ToolBtn" },
            Padding = new Thickness(4, 2),
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        ToolTip.SetTip(btn, "Stash local changes (dropdown for more options)");
        btn.Click += (_, _) => _ = ShowModuleDialogAsync(m => new StashDialog(m));
        return btn;
    }

    private void ShowRecentReposPopup(Control anchor)
    {
        var flyout = new MenuFlyout();
        var recent = App.Settings.GetStringList("recentRepositories");
        if (recent.Count == 0)
        {
            flyout.Items.Add(new MenuItem { Header = "(no recent repositories)", IsEnabled = false });
        }
        else
        {
            foreach (string path in recent)
            {
                string capturedPath = path;
                flyout.Items.Add(new MenuItem
                {
                    Header = capturedPath,
                    Command = ReactiveCommand.Create(() => OpenRepository(capturedPath)),
                });
            }
        }

        flyout.ShowAt(anchor);
    }

    private async System.Threading.Tasks.Task PullRebaseAsync()
    {
        if (_module is null)
        {
            return;
        }

        var capturedModule = _module;
        var dialog = new GitProgressDialog(
            "Pull (rebase)…",
            () => capturedModule.GitExecutable.GetOutputAsync("pull --rebase"));
        await dialog.ShowDialog<object?>(this);
        await RevisionGrid.RefreshAsync();
        await RefreshBranchSelectorAsync();
        await RefreshStatusBarCountsAsync();
        _ = LeftPanel.RefreshAsync();
    }

    private async System.Threading.Tasks.Task FetchPruneAsync()
    {
        if (_module is null)
        {
            return;
        }

        var capturedModule = _module;
        var dialog = new GitProgressDialog(
            "Fetching (pruning)…",
            () => capturedModule.GitExecutable.GetOutputAsync("fetch --all --prune"));
        await dialog.ShowDialog<object?>(this);
        await RevisionGrid.RefreshAsync();
        await RefreshActionBarsAsync();
        await RefreshStatusBarCountsAsync();
    }

    private async System.Threading.Tasks.Task StashStagedAsync()
    {
        if (_module is null)
        {
            return;
        }

        var capturedModule = _module;
        await System.Threading.Tasks.Task.Run(() =>
            capturedModule.GitExecutable.GetOutput("stash --staged"));
        await RevisionGrid.RefreshAsync();
        await RefreshStatusBarCountsAsync();
    }

    private async System.Threading.Tasks.Task StashPopAsync()
    {
        if (_module is null)
        {
            return;
        }

        var capturedModule = _module;
        await System.Threading.Tasks.Task.Run(() =>
            capturedModule.GitExecutable.GetOutput("stash pop"));
        await RevisionGrid.RefreshAsync();
        await RefreshStatusBarCountsAsync();
    }

    private async System.Threading.Tasks.Task RefreshStatusBarCountsAsync()
    {
        if (_module is null)
        {
            return;
        }

        var capturedModule = _module;
        var (ahead, behind, staged, unstaged) = await System.Threading.Tasks.Task.Run(() =>
        {
            int aheadCount = 0, behindCount = 0, stagedCount = 0, unstagedCount = 0;
            try
            {
                string aheadStr = capturedModule.GitExecutable.GetOutput("rev-list --count @{u}..HEAD 2>/dev/null").Trim();
                int.TryParse(aheadStr, out aheadCount);
            }
            catch
            {
            }

            try
            {
                string behindStr = capturedModule.GitExecutable.GetOutput("rev-list --count HEAD..@{u} 2>/dev/null").Trim();
                int.TryParse(behindStr, out behindCount);
            }
            catch
            {
            }

            try
            {
                string stagedStr = capturedModule.GitExecutable.GetOutput("diff --cached --name-only").Trim();
                stagedCount = string.IsNullOrEmpty(stagedStr) ? 0 : stagedStr.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
            }
            catch
            {
            }

            try
            {
                string unstagedStr = capturedModule.GitExecutable.GetOutput("diff --name-only").Trim();
                unstagedCount = string.IsNullOrEmpty(unstagedStr) ? 0 : unstagedStr.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
            }
            catch
            {
            }

            return (aheadCount, behindCount, stagedCount, unstagedCount);
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (ahead > 0 || behind > 0)
            {
                AheadBehindLabel.Text = $"↑{ahead} ↓{behind}";
                AheadBehindLabel.IsVisible = true;
            }
            else
            {
                AheadBehindLabel.IsVisible = false;
            }

            if (staged > 0)
            {
                StagedCountLabel.Text = $"●{staged} staged";
                StagedCountLabel.IsVisible = true;
            }
            else
            {
                StagedCountLabel.IsVisible = false;
            }

            if (unstaged > 0)
            {
                UnstagedCountLabel.Text = $"○{unstaged} unstaged";
                UnstagedCountLabel.IsVisible = true;
            }
            else
            {
                UnstagedCountLabel.IsVisible = false;
            }
        });
    }

    private static Image? MakeToolIcon(string assetUri)
    {
        try
        {
            var uri = new Uri(assetUri);
            using var stream = AssetLoader.Open(uri);
            var bitmap = new Bitmap(stream);
            return new Image { Source = bitmap, Width = 16, Height = 16 };
        }
        catch
        {
            return null;
        }
    }

    private static Button MakeToolButton(string assetUri, string fallbackText, string tooltip, Action onClick)
    {
        Control content = MakeToolIcon(assetUri) ?? (Control)new TextBlock { Text = fallbackText };
        var btn = new Button
        {
            Content = content,
            Classes = { "ToolBtn" },
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        ToolTip.SetTip(btn, tooltip);
        btn.Click += (_, _) => onClick();
        return btn;
    }

    private static Button MakeToolButton(string icon, string tooltip, Action onClick)
    {
        var btn = new Button
        {
            Content = icon,
            Classes = { "ToolBtn" },
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        ToolTip.SetTip(btn, tooltip);
        btn.Click += (_, _) => onClick();
        return btn;
    }

    private static Control MakeToolSeparator() =>
        new Border
        {
            Classes = { "ToolSeparator" },
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
                BranchLabel.Text = string.IsNullOrEmpty(currentBranch) ? string.Empty : $"⎇  {currentBranch}";
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

    private async System.Threading.Tasks.Task CheckoutHashAsync(string hash)
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.GitExecutable.GetOutputAsync($"checkout {hash}");
            StatusLabel.Text = $"Detached HEAD at {hash[..7]}";
            await RevisionGrid.RefreshAsync();
            await RefreshBranchSelectorAsync();
            await RefreshStatusBarCountsAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async System.Threading.Tasks.Task ResetHardAsync(string hash)
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.GitExecutable.GetOutputAsync($"reset --hard {hash}");
            StatusLabel.Text = $"Reset to {hash[..7]}";
            await RevisionGrid.RefreshAsync();
            await RefreshStatusBarCountsAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
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
            await _module.GitExecutable.GetOutputAsync($"checkout {branch.Quote()}");
            StatusLabel.Text = $"On branch {branch}";
            await RevisionGrid.RefreshAsync();
            await RefreshBranchSelectorAsync();
            await RefreshStatusBarCountsAsync();
            _ = LeftPanel.RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async System.Threading.Tasks.Task CheckoutRemoteBranchAsync(string remoteBranch)
    {
        if (_module is null)
        {
            return;
        }

        string localBranch = GetRemoteBranchLocalName(remoteBranch);
        StatusLabel.Text = $"Checking out {remoteBranch} as {localBranch}…";
        try
        {
            await _module.GitExecutable.GetOutputAsync($"checkout -b {localBranch.Quote()} {remoteBranch.Quote()}");
            StatusLabel.Text = $"On branch {localBranch}";
            await RevisionGrid.RefreshAsync();
            await RefreshBranchSelectorAsync();
            await RefreshStatusBarCountsAsync();
            _ = LeftPanel.RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private static string GetRemoteBranchLocalName(string remoteBranch)
    {
        int slashIndex = remoteBranch.IndexOf('/');
        return slashIndex >= 0 && slashIndex + 1 < remoteBranch.Length
            ? remoteBranch[(slashIndex + 1)..]
            : remoteBranch;
    }

    protected override void OnKeyDown(global::Avalonia.Input.KeyEventArgs e)
    {
        base.OnKeyDown(e);
        bool ctrl = (e.KeyModifiers & global::Avalonia.Input.KeyModifiers.Control) != 0;
        if (e.Key == global::Avalonia.Input.Key.F5 && HasRepository)
        {
            _ = RevisionGrid.RefreshAsync();
            e.Handled = true;
        }
        else if (ctrl && e.Key == global::Avalonia.Input.Key.Enter && HasRepository)
        {
            _ = ShowCommitDialogAsync();
            e.Handled = true;
        }
        else if (ctrl && e.Key == global::Avalonia.Input.Key.G && HasRepository)
        {
            _ = GoToCommitAsync();
            e.Handled = true;
        }
        else if (ctrl && e.Key == global::Avalonia.Input.Key.F && HasRepository)
        {
            FilterBar.Focus();
            e.Handled = true;
        }
        else if (ctrl && e.Key == global::Avalonia.Input.Key.K)
        {
            ToggleLeftPanel();
            e.Handled = true;
        }
        else if (e.Key == global::Avalonia.Input.Key.F12)
        {
            _ = ShowGitCommandLogAsync();
            e.Handled = true;
        }
    }

    private double _savedLeftPanelWidth = 220;

    private void ToggleLeftPanel()
    {
        var col = TopContentGrid.ColumnDefinitions[0];
        var splitter = TopContentGrid.ColumnDefinitions[1];
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
