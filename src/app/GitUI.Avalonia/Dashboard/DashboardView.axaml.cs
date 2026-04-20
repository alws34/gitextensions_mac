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
}
