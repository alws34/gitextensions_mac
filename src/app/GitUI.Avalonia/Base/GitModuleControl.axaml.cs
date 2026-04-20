using Avalonia.Controls;
using GitCommands;

namespace GitUI.Avalonia.Base;

/// <summary>
/// Base class for all embedded controls that need access to a GitModule.
/// Replaces WinForms GitModuleControl.
/// </summary>
public partial class GitModuleControl : UserControl
{
    private GitModule? _module;
    private IGitUICommandsSource? _uiCommandsSource;

    public GitModule? Module
    {
        get => _module;
        set
        {
            _module = value;
            if (value is not null)
            {
                OnModuleSet();
            }
        }
    }

    public IGitUICommandsSource? UICommandsSource
    {
        get => _uiCommandsSource;
        set
        {
            _uiCommandsSource = value;
            if (value is not null)
            {
                OnUICommandsSourceSet();
            }
        }
    }

    protected virtual void OnModuleSet()
    {
    }

    protected virtual void OnUICommandsSourceSet()
    {
    }
}
