# Settings Migration — Windows Registry → JSON Config

## Overview

GitExtensions on Windows stores settings in the Windows Registry and in `.settings` files. On Mac, all settings are stored in JSON at:

```
~/.config/gitextensions/settings.json
```

## Implementation

The existing `AppSettings` class in `GitCommands/Settings/AppSettings.cs` reads from Windows Registry. In the Mac fork, this is replaced by `AppSettingsMac` in `GitUI.Avalonia/Infrastructure/AppSettingsMac.cs`.

`AppSettingsMac` must implement all the same public properties as `AppSettings` but read/write from `~/.config/gitextensions/settings.json` using `Microsoft.Extensions.Configuration`.

The two classes share an interface `IAppSettings` (to be created during Task 1) so that `GitCommands` can reference settings without depending on the platform implementation.

## Config file location

```csharp
public static string ConfigPath =>
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "gitextensions", "settings.json");
```

Create the directory if it doesn't exist on first run.

## Key settings to migrate

| Setting name | Type | Default | Notes |
|-------------|------|---------|-------|
| `gitBinDir` | string | `""` | Path to git executable directory |
| `pullMerge` | enum | Merge | Pull strategy |
| `defaultCloneDestinationPath` | string | `""` | |
| `recentRepositories` | string[] | `[]` | List of recently opened repos |
| `commitInfoShowContainedInBranches` | bool | true | |
| `commitInfoShowContainedInTags` | bool | true | |
| `showGitStatusInBrowseToolbar` | bool | true | |
| `checkForUpdates` | bool | true | |
| `autoStash` | bool | false | |
| `defaultBranch` | string | `""` | |
| `encoding` | string | `"UTF-8"` | |
| `authorInitials` | string | `""` | |
| `diffAddedColor` | color | green | |
| `diffRemovedColor` | color | red | |
| `diffSectionColor` | color | yellow | |
| `graphBranchColors` | color[] | default palette | |

> **Note to agent:** Do not try to enumerate all settings upfront. Read `AppSettings.cs` and migrate settings as each UI screen needs them. Settings used by a screen should be migrated in the same task as that screen.

## JSON structure

```json
{
  "gitExtensions": {
    "gitBinDir": "/usr/bin",
    "pullMerge": "Merge",
    "recentRepositories": [
      "/Users/user/projects/myrepo"
    ],
    "ui": {
      "diffAddedColor": "#002200",
      "diffRemovedColor": "#220000"
    }
  }
}
```

## First-run defaults

On first launch (no config file exists), write a default config with sensible Mac defaults:

```json
{
  "gitExtensions": {
    "gitBinDir": "",
    "checkForUpdates": true,
    "encoding": "UTF-8"
  }
}
```

Then detect git automatically:
```csharp
// Try common Mac locations
var candidates = new[] {
    "/usr/bin/git",
    "/usr/local/bin/git",
    "/opt/homebrew/bin/git"
};
var gitPath = candidates.FirstOrDefault(File.Exists) ?? "";
```

## What to do with the `.settings` file

The existing `GitExtensions.settings` file in the repo root is for project-level settings (stored in the repo, not user settings). This file is fine as-is — it is XML, not Registry-based.
