using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class ScriptsManagerDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private List<UserScript> _scripts = [];

    public ScriptsManagerDialog(GitModule module)
    {
        _module = module;
        InitializeComponent();
        LoadScripts();
    }

    private void LoadScripts()
    {
        var raw = App.Settings.GetStringList("userScripts");
        _scripts = raw
            .Select(s =>
            {
                int sep = s.IndexOf("|||", StringComparison.Ordinal);
                return sep < 0
                    ? new UserScript(s, string.Empty)
                    : new UserScript(s[..sep], s[(sep + 3)..]);
            })
            .ToList();
        RefreshList();
    }

    private void SaveScripts()
    {
        var raw = _scripts.Select(s => $"{s.Name}|||{s.Command}").ToList();
        App.Settings.SetStringList("userScripts", raw);
        App.Settings.Save();
    }

    private void RefreshList()
    {
        ScriptsList.ItemsSource = _scripts.Select(s => s.Name).ToList();
    }

    private void ScriptsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        int idx = ScriptsList.SelectedIndex;
        if (idx < 0 || idx >= _scripts.Count)
        {
            return;
        }

        NameBox.Text = _scripts[idx].Name;
        CommandBox.Text = _scripts[idx].Command;
    }

    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        _scripts.Add(new UserScript("New Script", "echo hello"));
        RefreshList();
        ScriptsList.SelectedIndex = _scripts.Count - 1;
    }

    private void Remove_Click(object? sender, RoutedEventArgs e)
    {
        int idx = ScriptsList.SelectedIndex;
        if (idx >= 0 && idx < _scripts.Count)
        {
            _scripts.RemoveAt(idx);
            RefreshList();
        }
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        int idx = ScriptsList.SelectedIndex;
        if (idx >= 0 && idx < _scripts.Count)
        {
            _scripts[idx] = new UserScript(
                NameBox.Text?.Trim() ?? string.Empty,
                CommandBox.Text?.Trim() ?? string.Empty);
            SaveScripts();
            RefreshList();
            StatusLabel.Text = "Saved.";
        }
    }

    private void Run_Click(object? sender, RoutedEventArgs e)
    {
        _ = RunScriptAsync();
    }

    private async System.Threading.Tasks.Task RunScriptAsync()
    {
        string cmd = CommandBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(cmd))
        {
            return;
        }

        // Expand tokens
        string currentHash = await System.Threading.Tasks.Task.Run(
            () => _module.GitExecutable.GetOutput("rev-parse --short HEAD").Trim());
        cmd = cmd.Replace("{hash}", currentHash, StringComparison.OrdinalIgnoreCase)
                 .Replace("{repo}", _module.WorkingDir, StringComparison.OrdinalIgnoreCase);

        await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = "Running…");

        try
        {
            string result = await System.Threading.Tasks.Task.Run(() =>
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    WorkingDirectory = _module.WorkingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                psi.ArgumentList.Add("-c");
                psi.ArgumentList.Add(cmd);

                using var process = System.Diagnostics.Process.Start(psi);
                if (process is null)
                {
                    return "Could not start process.";
                }

                string output = process.StandardOutput.ReadToEnd();
                string err = process.StandardError.ReadToEnd();
                process.WaitForExit();
                return string.IsNullOrWhiteSpace(output) ? err.Trim() : output.Trim();
            });

            await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = result.Length > 80 ? result[..80] + "…" : result);
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = $"Error: {ex.Message}");
        }
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close(null);

    private sealed record UserScript(string Name, string Command);
}
