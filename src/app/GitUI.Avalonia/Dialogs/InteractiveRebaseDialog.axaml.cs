using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class InteractiveRebaseDialog : GitExtensionsDialog
{
    private readonly GitModule _module;
    private readonly string _baseCommit;
    private readonly ObservableCollection<RebaseCommit> _commits = [];

    public InteractiveRebaseDialog(GitModule module, string baseCommit)
    {
        _module = module;
        _baseCommit = baseCommit;
        InitializeComponent();
        InfoLabel.Text = $"Rebasing commits on top of: {baseCommit}";
        CommitList.ItemsSource = _commits;
        _ = LoadCommitsAsync();
    }

    private async System.Threading.Tasks.Task LoadCommitsAsync()
    {
        var commits = await System.Threading.Tasks.Task.Run(() =>
        {
            string log = _module.GitExecutable.GetOutput(
                $"log --format=%H|%s {_baseCommit}..HEAD --reverse");
            return log.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                      .Select(line =>
                      {
                          int pipe = line.IndexOf('|');
                          if (pipe < 0)
                          {
                              return new RebaseCommit(line.Trim(), string.Empty);
                          }

                          return new RebaseCommit(line[..pipe].Trim(), line[(pipe + 1)..].Trim());
                      })
                      .ToList();
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _commits.Clear();
            foreach (var c in commits)
            {
                _commits.Add(c);
            }
        });
    }

    private void MoveUp_Click(object? sender, RoutedEventArgs e)
    {
        int idx = CommitList.SelectedIndex;
        if (idx > 0)
        {
            var item = _commits[idx];
            _commits.RemoveAt(idx);
            _commits.Insert(idx - 1, item);
            CommitList.SelectedIndex = idx - 1;
        }
    }

    private void MoveDown_Click(object? sender, RoutedEventArgs e)
    {
        int idx = CommitList.SelectedIndex;
        if (idx >= 0 && idx < _commits.Count - 1)
        {
            var item = _commits[idx];
            _commits.RemoveAt(idx);
            _commits.Insert(idx + 1, item);
            CommitList.SelectedIndex = idx + 1;
        }
    }

    private void Start_Click(object? sender, RoutedEventArgs e)
    {
        _ = StartRebaseAsync();
    }

    private async System.Threading.Tasks.Task StartRebaseAsync()
    {
        if (_commits.Count == 0)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = "Starting rebase…");

        try
        {
            // Build the rebase-todo content
            var lines = _commits.Select(c => $"{c.Action} {c.Hash} {c.Subject}");
            string todoContent = string.Join("\n", lines) + "\n";

            // Write todo to temp file
            string todoPath = System.IO.Path.GetTempFileName();
            await System.Threading.Tasks.Task.Run(() => System.IO.File.WriteAllText(todoPath, todoContent));

            // Write a shell script that copies our todo to the git sequence editor target
            string scriptPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "ge_rebase_editor.sh");
            string scriptContent = $"#!/bin/sh\ncp \"{todoPath}\" \"$1\"\n";
            await System.Threading.Tasks.Task.Run(() =>
            {
                System.IO.File.WriteAllText(scriptPath, scriptContent);
                var chmod = System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "chmod",
                        ArgumentList = { "+x", scriptPath },
                        UseShellExecute = false,
                    });
                chmod?.WaitForExit();
            });

            // Run git rebase -i with our script as sequence editor
            string result = await System.Threading.Tasks.Task.Run(() =>
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    WorkingDirectory = _module.WorkingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                psi.Environment["GIT_SEQUENCE_EDITOR"] = scriptPath;
                psi.ArgumentList.Add("rebase");
                psi.ArgumentList.Add("-i");
                psi.ArgumentList.Add(_baseCommit);

                using var process = System.Diagnostics.Process.Start(psi);
                if (process is null)
                {
                    return "Could not start git process.";
                }

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();
                return process.ExitCode == 0
                    ? (string.IsNullOrWhiteSpace(stdout) ? "Rebase completed." : stdout.Trim())
                    : $"Rebase failed:\n{stderr.Trim()}";
            });

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusLabel.Text = result.StartsWith("Rebase failed", StringComparison.Ordinal)
                    ? "✗ Failed — resolve conflicts then use Rebase dialog to continue"
                    : "✓ Rebase complete";
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = $"✗ Error: {ex.Message}");
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);
}

public sealed class RebaseCommit : INotifyPropertyChanged
{
    private string _action = "pick";

    public RebaseCommit(string hash, string subject)
    {
        Hash = hash;
        Subject = subject;
    }

    public string Hash { get; }
    public string ShortHash => Hash.Length >= 7 ? Hash[..7] : Hash;
    public string Subject { get; }

    public IReadOnlyList<string> Actions { get; } =
        ["pick", "reword", "edit", "squash", "fixup", "drop"];

    public string Action
    {
        get => _action;
        set
        {
            if (_action != value)
            {
                _action = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
