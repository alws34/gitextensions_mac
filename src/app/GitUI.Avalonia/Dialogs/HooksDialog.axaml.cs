using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GitCommands;
using GitUI.Avalonia.Base;

namespace GitUI.Avalonia.Dialogs;

public partial class HooksDialog : GitExtensionsDialog
{
    private readonly string _hooksDir;
    private List<HookInfo> _hooks = [];

    public HooksDialog(GitModule module)
    {
        _hooksDir = Path.Combine(module.WorkingDirGitDir, "hooks");
        InitializeComponent();
        LoadHooks();
    }

    private void LoadHooks()
    {
        _hooks = [];

        // Standard git hook names
        string[] standardHooks =
        [
            "pre-commit", "prepare-commit-msg", "commit-msg", "post-commit",
            "pre-push", "pre-rebase", "post-checkout", "post-merge",
            "pre-receive", "update", "post-receive", "post-update",
            "pre-auto-gc", "post-rewrite", "applypatch-msg",
            "pre-applypatch", "post-applypatch",
        ];

        foreach (string name in standardHooks)
        {
            string activePath = Path.Combine(_hooksDir, name);
            string samplePath = Path.Combine(_hooksDir, name + ".sample");

            bool active = File.Exists(activePath);
            bool hasSample = File.Exists(samplePath);
            _hooks.Add(new HookInfo(name, activePath, samplePath, active, hasSample));
        }

        // Also include any non-standard active hooks
        if (Directory.Exists(_hooksDir))
        {
            foreach (string file in Directory.GetFiles(_hooksDir))
            {
                string hookName = Path.GetFileName(file);
                if (!hookName.EndsWith(".sample", StringComparison.Ordinal)
                    && _hooks.All(h => h.Name != hookName))
                {
                    _hooks.Add(new HookInfo(hookName, file, null, true, false));
                }
            }
        }

        HooksList.ItemsSource = _hooks
            .Select(h => $"{(h.IsActive ? "✓" : "○")} {h.Name}")
            .ToList();
    }

    private void HooksList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        int idx = HooksList.SelectedIndex;
        if (idx < 0 || idx >= _hooks.Count)
        {
            return;
        }

        HookInfo hook = _hooks[idx];
        HookStatusLabel.Text = hook.IsActive ? "● Active" : "○ Inactive";

        if (hook.IsActive && File.Exists(hook.ActivePath))
        {
            HookEditor.Text = File.ReadAllText(hook.ActivePath);
        }
        else if (hook.HasSample && hook.SamplePath is not null && File.Exists(hook.SamplePath))
        {
            HookEditor.Text = File.ReadAllText(hook.SamplePath);
        }
        else
        {
            HookEditor.Text = $"#!/bin/sh\n# {hook.Name} hook\n";
        }
    }

    private void Enable_Click(object? sender, RoutedEventArgs e)
    {
        int idx = HooksList.SelectedIndex;
        if (idx < 0 || idx >= _hooks.Count)
        {
            return;
        }

        HookInfo hook = _hooks[idx];
        if (hook.IsActive)
        {
            return;
        }

        Directory.CreateDirectory(_hooksDir);

        // Copy sample or write editor content
        string content = HookEditor.Text ?? $"#!/bin/sh\n# {hook.Name}\n";
        File.WriteAllText(hook.ActivePath, content);

        // Make executable
        var chmod = System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo
            {
                FileName = "chmod",
                ArgumentList = { "+x", hook.ActivePath },
                UseShellExecute = false,
            });
        chmod?.WaitForExit();

        StatusLabel.Text = $"Enabled {hook.Name}";
        LoadHooks();
        HooksList.SelectedIndex = idx;
    }

    private void Disable_Click(object? sender, RoutedEventArgs e)
    {
        int idx = HooksList.SelectedIndex;
        if (idx < 0 || idx >= _hooks.Count)
        {
            return;
        }

        HookInfo hook = _hooks[idx];
        if (!hook.IsActive || !File.Exists(hook.ActivePath))
        {
            return;
        }

        File.Move(hook.ActivePath, hook.ActivePath + ".disabled", overwrite: true);
        StatusLabel.Text = $"Disabled {hook.Name}";
        LoadHooks();
        HooksList.SelectedIndex = idx;
    }

    private void SaveHook_Click(object? sender, RoutedEventArgs e)
    {
        int idx = HooksList.SelectedIndex;
        if (idx < 0 || idx >= _hooks.Count)
        {
            return;
        }

        HookInfo hook = _hooks[idx];
        if (!hook.IsActive)
        {
            StatusLabel.Text = "Enable the hook first before saving.";
            return;
        }

        Directory.CreateDirectory(_hooksDir);
        File.WriteAllText(hook.ActivePath, HookEditor.Text ?? string.Empty);
        StatusLabel.Text = $"Saved {hook.Name}";
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close(null);

    private sealed record HookInfo(
        string Name,
        string ActivePath,
        string? SamplePath,
        bool IsActive,
        bool HasSample);
}
