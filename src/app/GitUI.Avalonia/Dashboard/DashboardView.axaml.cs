using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GitUI.Avalonia.Dashboard;

public record RecentRepo(string Path)
{
    public string Name => System.IO.Path.GetFileName(Path.TrimEnd('/', '\\'));
}

public partial class DashboardView : UserControl
{
    public event Action<string>? OnOpenRepository;

    /// <summary>Called when the user wants to clone a repository.</summary>
    public Action? OnClone { get; set; }

    /// <summary>Called when the user wants to init a new repository.</summary>
    public Action? OnInit { get; set; }

    /// <summary>Called when the user clicks "Browse for repository…".</summary>
    public Action? OnBrowse { get; set; }

    private IList<RecentRepo> _recentRepositories = [];

    public IList<RecentRepo> RecentRepositories
    {
        get => _recentRepositories;
        set
        {
            _recentRepositories = value;
            RecentList.ItemsSource = value;
        }
    }

    public DashboardView() => InitializeComponent();

    /// <summary>Reloads the recent repository list from settings.</summary>
    public void Refresh()
    {
        RecentList.SelectedItem = null;
        var recent = App.Settings.GetStringList("recentRepositories");
        RecentRepositories = recent.Select(p => new RecentRepo(p)).ToList();
    }

    private void RecentList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (RecentList.SelectedItem is RecentRepo repo)
        {
            OnOpenRepository?.Invoke(repo.Path);
        }
    }

    private void OpenButton_Click(object? sender, RoutedEventArgs e)
    {
        _ = OpenRepositoryAsync();
    }

    private async System.Threading.Tasks.Task OpenRepositoryAsync()
    {
        var dialog = new OpenRepositoryDialog();
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is Window win)
        {
            var path = await dialog.ShowDialog<string?>(win);
            if (path is not null)
            {
                OnOpenRepository?.Invoke(path);
            }
        }
    }

    private void CloneButton_Click(object? sender, RoutedEventArgs e)
    {
        OnClone?.Invoke();
    }

    private void InitButton_Click(object? sender, RoutedEventArgs e)
    {
        OnInit?.Invoke();
    }

    private void BrowseButton_Click(object? sender, RoutedEventArgs e)
    {
        if (OnBrowse is not null)
        {
            OnBrowse.Invoke();
        }
        else
        {
            // Fallback: use built-in open dialog
            _ = OpenRepositoryAsync();
        }
    }
}
