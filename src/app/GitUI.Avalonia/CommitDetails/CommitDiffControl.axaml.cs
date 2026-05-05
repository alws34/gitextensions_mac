using Avalonia.Threading;
using GitCommands;
using GitUI.Avalonia.Base;
using GitUIPluginInterfaces;

namespace GitUI.Avalonia.CommitDetails;

public partial class CommitDiffControl : GitModuleControl
{
    private bool _isSplitView;
    private string _lastDiff = string.Empty;
    private string? _lastFilePath;

    public CommitDiffControl()
    {
        InitializeComponent();
        _isSplitView = GetSettingBool("diffSplitViewDefault", false);
        DiffEditor.IsVisible = !_isSplitView;
        SplitView.IsVisible = _isSplitView;
        ApplyEditorSettings();
        DiffEditor.TextArea.TextView.LineTransformers.Add(new DiffLineColorizer());
        LeftEditor.TextArea.TextView.LineTransformers.Add(new DiffLineColorizer());
        RightEditor.TextArea.TextView.LineTransformers.Add(new DiffLineColorizer());
        UpdateToolbarState();
    }

    private void ApplyEditorSettings()
    {
        bool wordWrap = GetSettingBool("diffWordWrap", false);
        DiffEditor.WordWrap = wordWrap;
        LeftEditor.WordWrap = wordWrap;
        RightEditor.WordWrap = wordWrap;
    }

    public void ApplySettings()
    {
        bool splitView = GetSettingBool("diffSplitViewDefault", false);
        SetSplitView(splitView);
        ApplyEditorSettings();
        ApplyDiff(_lastDiff, _lastFilePath);
    }

    private void UpdateToolbarState()
    {
        if (_isSplitView)
        {
            UnifiedButton.Classes.Remove("Active");
            SplitButton.Classes.Add("Active");
        }
        else
        {
            UnifiedButton.Classes.Add("Active");
            SplitButton.Classes.Remove("Active");
        }
    }

    public async System.Threading.Tasks.Task ShowDiffAsync(GitRevision revision, string? filePath = null)
    {
        if (Module is null)
        {
            return;
        }

        try
        {
            string gitArgs = BuildDiffTreeArguments(revision.Guid, filePath);

            string diff = await Module.GitExecutable.GetOutputAsync(gitArgs);
            _lastDiff = diff;
            _lastFilePath = filePath;

            await Dispatcher.UIThread.InvokeAsync(() => ApplyDiff(diff, filePath));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShowDiffAsync failed: {ex.Message}");
        }
    }

    private void ApplyDiff(string diff, string? filePath)
    {
        if (_isSplitView)
        {
            ApplySplitDiff(diff, filePath);
        }
        else
        {
            DiffEditor.Text = diff;
            ApplySyntaxHighlighting(DiffEditor, filePath);
        }
    }

    private void ApplySplitDiff(string diff, string? filePath)
    {
        var rows = SideBySideDiffParser.Parse(diff);
        string leftText = string.Join("\n", rows.Select(r => r.Left ?? string.Empty));
        string rightText = string.Join("\n", rows.Select(r => r.Right ?? string.Empty));
        LeftEditor.Text = leftText;
        RightEditor.Text = rightText;
        ApplySyntaxHighlighting(LeftEditor, filePath);
        ApplySyntaxHighlighting(RightEditor, filePath);
    }

    private static void ApplySyntaxHighlighting(AvaloniaEdit.TextEditor editor, string? filePath)
    {
        if (!GetSettingBool("diffSyntaxHighlighting", true))
        {
            editor.SyntaxHighlighting = null;
            return;
        }

        if (string.IsNullOrEmpty(filePath))
        {
            editor.SyntaxHighlighting = null;
            return;
        }

        string ext = System.IO.Path.GetExtension(filePath);
        if (string.IsNullOrEmpty(ext))
        {
            editor.SyntaxHighlighting = null;
            return;
        }

        var highlighting = AvaloniaEdit.Highlighting.HighlightingManager.Instance
            .GetDefinitionByExtension(ext);
        editor.SyntaxHighlighting = highlighting;
    }

    private void Unified_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        SetSplitView(false);
    }

    private void Split_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        SetSplitView(true);
    }

    private void SetSplitView(bool splitView)
    {
        if (_isSplitView == splitView)
        {
            return;
        }

        _isSplitView = splitView;
        DiffEditor.IsVisible = !_isSplitView;
        SplitView.IsVisible = _isSplitView;
        UpdateToolbarState();
        ApplyDiff(_lastDiff, _lastFilePath);
    }

    internal static string BuildDiffTreeArguments(string revisionGuid, string? filePath)
        => string.IsNullOrWhiteSpace(filePath)
            ? $"diff-tree --no-commit-id -p --root -r {revisionGuid}"
            : $"diff-tree --no-commit-id -p --root -r {revisionGuid} -- {filePath.Quote()}";

    private static bool GetSettingBool(string key, bool defaultValue)
        => App.Settings is null ? defaultValue : App.Settings.GetBool(key, defaultValue);
}
