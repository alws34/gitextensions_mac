# Mac Port — Plan D: Screen Wave 2 + Plugins

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port all remaining helper dialogs, small git dialogs, diff/blame/file history views, and all plugins. Final integration, distribution build, and CI setup.

**Architecture:** Tasks D1–D5 are independent (parallelizable). Tasks D6–D8 must run after D1–D5 (integration + distribution). Plugin tasks (D9+) depend on the core being complete.

**Prerequisite:** Plans A, B, and C must be complete.

**Reference docs:**
- `docs/mac-port/AGENT-GUIDE.md`
- `docs/mac-port/WINFORMS-TO-AVALONIA.md`
- `docs/superpowers/specs/2026-04-19-mac-port-design.md` Sections 6, 8, 9

---

## Task D1: Diff & File View Dialogs

**Source files:** `FormDiff.cs`, `FormFileHistory.cs`, `FormBlame.cs`, `FormEditor.cs`, `FormLog.cs`

All use AvaloniaEdit for the text view. Follow the standard dialog pattern from Plan C.

- [ ] **Step 1: Create `Dialogs/DiffDialog.axaml` + `.axaml.cs`**

Layout:
- Top: ComboBox for "diff A" and "diff B" (two refs/commits), file filter TextBox
- Bottom: AvaloniaEdit in read-only mode showing the diff output

```csharp
// Load diff
var diff = await Task.Run(() =>
    _module.GitExecutable.GetOutput($"diff {refA} {refB}"));
DiffEditor.Text = diff;
```

- [ ] **Step 2: Create `Dialogs/FileHistoryDialog.axaml` + `.axaml.cs`**

Layout:
- Top half: `RevisionGridControl` (from Plan B) showing only commits that touched the file
- Bottom half: AvaloniaEdit showing the file at the selected revision

Pass a file path to `RevisionGridControl` to filter commits:
```csharp
RevisionGrid.Module = _module;
RevisionGrid.SetFileFilter(filePath);  // check actual method name
```

- [ ] **Step 3: Create `Dialogs/BlameDialog.axaml` + `.axaml.cs`**

Layout: AvaloniaEdit showing file contents, each line annotated with commit hash and author in the left margin (use AvaloniaEdit's custom margin API).

Load with: `git blame --line-porcelain <file>`

- [ ] **Step 4: Create `Dialogs/EditorDialog.axaml` + `.axaml.cs`**

A general-purpose text editor dialog. Layout: AvaloniaEdit (editable), Save/Cancel buttons.
Used for editing `.gitignore`, `.gitattributes`, commit templates, etc.

- [ ] **Step 5: Create `Dialogs/LogDialog.axaml` + `.axaml.cs`**

Layout: AvaloniaEdit in read-only mode showing `git log` output with formatting options (--oneline, --graph, --all checkboxes).

- [ ] **Step 6: Wire into Commands menu**

Add: View Diff, File History (requires selected file), Blame, Git Log.

- [ ] **Step 7: Commit**

```bash
git add src/app/GitUI.Avalonia/Dialogs/Diff* \
        src/app/GitUI.Avalonia/Dialogs/FileHistory* \
        src/app/GitUI.Avalonia/Dialogs/Blame* \
        src/app/GitUI.Avalonia/Dialogs/Editor* \
        src/app/GitUI.Avalonia/Dialogs/Log*
git commit -m "feat: add diff, file history, blame, editor, log dialogs"
```

---

## Task D2: Helper and Process Dialogs

**Source files:** `HelperDialogs/FormStatus.cs`, `FormProcess.cs`, `FormRemoteProcess.cs`, `FormEdit.cs`, `FormChooseCommit.cs`, `FormSelectMultipleBranches.cs`

- [ ] **Step 1: Create `Dialogs/StatusDialog.axaml` + `.axaml.cs`**

Shows progress of a long-running git operation (e.g., clone, fetch). Layout: progress bar, scrollable output log (read-only TextBlock), Close button (enabled after operation completes).

```csharp
public class StatusDialog : GitExtensionsDialog
{
    public StatusDialog(string title, Func<IProgress<string>, Task> operation)
    {
        Title = title;
        InitializeComponent();
        _ = RunAsync(operation);
    }

    private async Task RunAsync(Func<IProgress<string>, Task> operation)
    {
        var progress = new Progress<string>(line =>
            OutputLog.Text += line + "\n");

        try
        {
            await operation(progress);
            CloseButton.IsEnabled = true;
            StatusLabel.Text = "Done.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            CloseButton.IsEnabled = true;
        }
    }
}
```

- [ ] **Step 2: Create `Dialogs/ChooseCommitDialog.axaml` + `.axaml.cs`**

Layout: search TextBox + `RevisionGridControl` (filtered by search). Returns selected `GitRevision`.

```csharp
// Usage
var dialog = new ChooseCommitDialog(_module);
var revision = await dialog.ShowDialog<GitRevision?>(parentWindow);
```

- [ ] **Step 3: Create `Dialogs/SelectMultipleBranchesDialog.axaml` + `.axaml.cs`**

Layout: ListBox with checkboxes for each branch (multi-select), OK button. Returns `IReadOnlyList<string>`.

- [ ] **Step 4: Create `Controls/StatusOutputLog.axaml` + `.axaml.cs`**

Reusable control for showing streaming git output. Used by StatusDialog and embedded in MainWindow status area.

Layout: scrollable read-only TextBlock with auto-scroll to bottom.

- [ ] **Step 5: Commit**

```bash
git add src/app/GitUI.Avalonia/Dialogs/Status* \
        src/app/GitUI.Avalonia/Dialogs/ChooseCommit* \
        src/app/GitUI.Avalonia/Dialogs/SelectMultiple* \
        src/app/GitUI.Avalonia/Controls/StatusOutputLog*
git commit -m "feat: add helper dialogs (status/progress, choose commit, multi-branch select)"
```

---

## Task D3: Small Git Dialogs

**Source files:** `FormAddFiles.cs`, `FormAddToGitIgnore.cs`, `FormGoToCommit.cs`, `FormFormatPatch.cs`, `FormCommandlineHelp.cs`, browse dialog helpers

All very simple — 1–3 controls each. Follow standard pattern.

- [ ] **Step 1: `Dialogs/AddFilesDialog.axaml` + `.axaml.cs`**

Layout: file pattern TextBox (default `*`), force flag checkbox, Add button.
Action: `git add <pattern>`

- [ ] **Step 2: `Dialogs/AddToGitIgnoreDialog.axaml` + `.axaml.cs`**

Layout: shows the file/pattern being added, target `.gitignore` ComboBox (repo-level vs global), Add button.

- [ ] **Step 3: `Dialogs/GoToCommitDialog.axaml` + `.axaml.cs`**

Layout: TextBox for commit SHA or ref name, Go button. Fires event to select that commit in the graph.

- [ ] **Step 4: `Dialogs/CommandlineHelpDialog.axaml` + `.axaml.cs`**

Layout: Read-only TextBlock showing git command help. Populated with `git <command> --help`.

- [ ] **Step 5: `Dialogs/AboutDialog.axaml` + `.axaml.cs`**

Layout: App logo, version, website link, license summary, contributors button.
Wire Help > About to open this dialog.

- [ ] **Step 6: `Dialogs/ChangeLogDialog.axaml` + `.axaml.cs`**

Layout: AvaloniaEdit showing CHANGELOG.md contents.

- [ ] **Step 7: `Dialogs/UpdatesDialog.axaml` + `.axaml.cs`**

Layout: current version, available version (fetched from GitHub releases API), download link.

- [ ] **Step 8: Commit**

```bash
git add src/app/GitUI.Avalonia/Dialogs/AddFiles* \
        src/app/GitUI.Avalonia/Dialogs/AddToGitIgnore* \
        src/app/GitUI.Avalonia/Dialogs/GoToCommit* \
        src/app/GitUI.Avalonia/Dialogs/CommandlineHelp* \
        src/app/GitUI.Avalonia/Dialogs/About* \
        src/app/GitUI.Avalonia/Dialogs/ChangeLog* \
        src/app/GitUI.Avalonia/Dialogs/Updates*
git commit -m "feat: add small git dialogs (add files, gitignore, go-to-commit, about, updates)"
```

---

## Task D4: Sparse Checkout and Advanced Dialogs

**Source files:** `FormSparseWorkingCopy.cs`, `FormMailMap.cs`, `FormCompareToBranch.cs`, `FormResetChanges.cs`

- [ ] **Step 1: `Dialogs/SparseWorkingCopyDialog.axaml` + `.axaml.cs`**

Layout: enable/disable toggle, patterns ListBox (add/remove), Apply button.

- [ ] **Step 2: `Dialogs/MailMapDialog.axaml` + `.axaml.cs`**

Layout: AvaloniaEdit showing `.mailmap` file contents, Save button.

- [ ] **Step 3: `Dialogs/CompareToBranchDialog.axaml` + `.axaml.cs`**

Layout: branch ComboBox, diff view below.

- [ ] **Step 4: `Dialogs/ResetChangesDialog.axaml` + `.axaml.cs`**

Layout: Shows staged/unstaged file counts, reset mode ComboBox (soft/mixed/hard), warning label for hard reset, Reset button.

- [ ] **Step 5: Commit**

```bash
git add src/app/GitUI.Avalonia/Dialogs/Sparse* \
        src/app/GitUI.Avalonia/Dialogs/MailMap* \
        src/app/GitUI.Avalonia/Dialogs/CompareTo* \
        src/app/GitUI.Avalonia/Dialogs/Reset*
git commit -m "feat: add sparse checkout, mailmap, compare-to-branch, reset dialogs"
```

---

## Task D5: SSH and Putty Dialogs

**Source files:** `FormPuttyError.cs`, `BrowseForPrivateKey.cs`, `HelperDialogs/FormBuildServerCredentials.cs`

- [ ] **Step 1: `Dialogs/PuttyErrorDialog.axaml` + `.axaml.cs`**

Simple error dialog: shows SSH/Putty error message, "Run Pageant" button (launch `ssh-agent` on Mac instead), Close button.

- [ ] **Step 2: `Dialogs/BrowseForPrivateKeyDialog.axaml` + `.axaml.cs`**

Layout: file picker pre-filtered to `~/.ssh/*.pem,*.key,*_rsa,*_ed25519`, path display, OK button.

- [ ] **Step 3: `Dialogs/BuildServerCredentialsDialog.axaml` + `.axaml.cs`**

Layout: URL TextBox, Username TextBox, Password TextBox (masked), Save button.

- [ ] **Step 4: Commit**

```bash
git add src/app/GitUI.Avalonia/Dialogs/Putty* \
        src/app/GitUI.Avalonia/Dialogs/BrowseForPrivateKey* \
        src/app/GitUI.Avalonia/Dialogs/BuildServerCredentials*
git commit -m "feat: add SSH key and build server credential dialogs"
```

---

## Task D6: Final Integration Pass

Run after D1–D5 are all complete. Wire every dialog into menus, verify the complete feature set.

- [ ] **Step 1: Audit menu coverage**

Open `MainWindow.axaml.cs`. For every dialog created in Plans B, C, D — verify there is a menu item that opens it. Add any that are missing.

- [ ] **Step 2: Verify zero WinForms references**

```bash
grep -r "System.Windows.Forms\|System.Drawing\|WindowsAPICodePack\|AdysTech\|ConEmu" \
     src/app/GitUI.Avalonia/ src/app/GitCommands/
```

Expected: zero results.

- [ ] **Step 3: Run full test suite**

```bash
dotnet test GitExtensions.Mac.slnx
```

Expected: all tests pass.

- [ ] **Step 4: Run the app and exercise every menu item**

Go through every menu item, open every dialog. Log any that crash or show blank.

- [ ] **Step 5: Fix any issues found in Step 4**

- [ ] **Step 6: Commit**

```bash
git add src/app/GitUI.Avalonia/
git commit -m "feat: complete menu wiring, all dialogs accessible from MainWindow"
```

---

## Task D7: macOS App Bundle and Distribution

- [ ] **Step 1: Create `Info.plist`**

Create `src/app/GitUI.Avalonia/Info.plist`:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleIdentifier</key>
    <string>com.gitextensions.gitextensions</string>
    <key>CFBundleName</key>
    <string>Git Extensions</string>
    <key>CFBundleDisplayName</key>
    <string>Git Extensions</string>
    <key>CFBundleVersion</key>
    <string>4.0.0</string>
    <key>CFBundleShortVersionString</key>
    <string>4.0.0</string>
    <key>CFBundleExecutable</key>
    <string>GitExtensions</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>LSMinimumSystemVersion</key>
    <string>12.0</string>
    <key>NSHumanReadableCopyright</key>
    <string>Copyright © 2024 Git Extensions Contributors</string>
</dict>
</plist>
```

- [ ] **Step 2: Create build script `scripts/build-mac.sh`**

```bash
#!/bin/bash
set -e

VERSION=${1:-"4.0.0"}
ARCH=${2:-"arm64"}  # or x64

echo "Building GitExtensions Mac $VERSION for $ARCH..."

# Publish
dotnet publish GitExtensions.Mac.slnx \
  -r "osx-$ARCH" \
  --self-contained true \
  -c Release \
  -p:Version=$VERSION \
  -o "artifacts/publish/osx-$ARCH"

# Create .app bundle structure
APP_DIR="artifacts/GitExtensions.app"
rm -rf "$APP_DIR"
mkdir -p "$APP_DIR/Contents/MacOS"
mkdir -p "$APP_DIR/Contents/Resources"

# Copy published files
cp -r "artifacts/publish/osx-$ARCH/." "$APP_DIR/Contents/MacOS/"

# Copy Info.plist
cp "src/app/GitUI.Avalonia/Info.plist" "$APP_DIR/Contents/Info.plist"

# Make executable
chmod +x "$APP_DIR/Contents/MacOS/GitUI.Avalonia"

# Rename executable to match CFBundleExecutable
mv "$APP_DIR/Contents/MacOS/GitUI.Avalonia" "$APP_DIR/Contents/MacOS/GitExtensions"

echo "App bundle created at $APP_DIR"
```

```bash
chmod +x scripts/build-mac.sh
```

- [ ] **Step 3: Test the app bundle**

```bash
./scripts/build-mac.sh 4.0.0 arm64
open artifacts/GitExtensions.app
```

Expected: App opens from the bundle (not from `dotnet run`).

- [ ] **Step 4: Create DMG build script `scripts/package-dmg.sh`**

```bash
#!/bin/bash
set -e

VERSION=${1:-"4.0.0"}

# Requires: brew install create-dmg
if ! command -v create-dmg &>/dev/null; then
    echo "Installing create-dmg..."
    brew install create-dmg
fi

create-dmg \
  --volname "Git Extensions" \
  --volicon "src/app/GitUI.Avalonia/Resources/AppIcon.icns" \
  --window-pos 200 120 \
  --window-size 600 400 \
  --icon-size 128 \
  --icon "GitExtensions.app" 150 200 \
  --hide-extension "GitExtensions.app" \
  --app-drop-link 450 200 \
  "artifacts/GitExtensions-$VERSION-mac.dmg" \
  "artifacts/GitExtensions.app"

echo "DMG created: artifacts/GitExtensions-$VERSION-mac.dmg"
```

- [ ] **Step 5: Convert app icon**

```bash
# Convert existing icon to ICNS format
# Source: src/app/GitUI/Properties/Icons/
sips -s format png src/app/GitUI/Properties/Icons/gitextensions.ico \
     --out artifacts/icon.png
mkdir -p artifacts/AppIcon.iconset
for size in 16 32 64 128 256 512; do
    sips -z $size $size artifacts/icon.png \
         --out "artifacts/AppIcon.iconset/icon_${size}x${size}.png"
done
iconutil -c icns artifacts/AppIcon.iconset \
         -o src/app/GitUI.Avalonia/Resources/AppIcon.icns
```

- [ ] **Step 6: Commit**

```bash
git add scripts/ src/app/GitUI.Avalonia/Info.plist src/app/GitUI.Avalonia/Resources/
git commit -m "feat: add macOS app bundle scripts and Info.plist"
```

---

## Task D8: GitHub Actions CI

- [ ] **Step 1: Create `.github/workflows/mac-build.yml`**

```yaml
name: Mac Build

on:
  push:
    branches: [master, main]
  pull_request:
    branches: [master, main]

jobs:
  build:
    runs-on: macos-latest

    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore GitExtensions.Mac.slnx

      - name: Build
        run: dotnet build GitExtensions.Mac.slnx --no-restore -c Release

      - name: Test
        run: dotnet test GitExtensions.Mac.slnx --no-build -c Release

  publish:
    runs-on: macos-latest
    needs: build
    if: github.ref == 'refs/heads/master' && github.event_name == 'push'

    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Publish arm64
        run: |
          dotnet publish GitExtensions.Mac.slnx \
            -r osx-arm64 --self-contained true -c Release \
            -o artifacts/publish/osx-arm64

      - name: Publish x64
        run: |
          dotnet publish GitExtensions.Mac.slnx \
            -r osx-x64 --self-contained true -c Release \
            -o artifacts/publish/osx-x64

      - name: Upload artifacts
        uses: actions/upload-artifact@v4
        with:
          name: GitExtensions-mac
          path: artifacts/publish/
```

- [ ] **Step 2: Verify CI passes**

Push to the fork. Check GitHub Actions tab — build and test must be green.

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/mac-build.yml
git commit -m "ci: add GitHub Actions workflow for macOS build and publish"
```

---

## Task D9: Plugins

**Prerequisite:** All Tasks D1–D8 complete.

Port each plugin. The business logic (non-UI) in each plugin is already cross-platform — only the settings/UI pages need Avalonia versions.

### D9.1: BackgroundFetch Plugin

**Source:** `src/plugins/BackgroundFetch/`

- [ ] **Step 1:** Read the plugin's `GitExtensionsPluginAttribute`-decorated class to understand how it integrates.
- [ ] **Step 2:** Port the settings page from WinForms UserControl to Avalonia UserControl.
- [ ] **Step 3:** Verify the plugin loads and its background fetch runs.
- [ ] **Commit:** `git commit -m "feat(plugin): port BackgroundFetch to Avalonia"`

### D9.2: DeleteUnusedBranches Plugin

**Source:** `src/plugins/DeleteUnusedBranches/`

- [ ] Port the dialog showing unused branches list with checkboxes.
- [ ] **Commit:** `git commit -m "feat(plugin): port DeleteUnusedBranches to Avalonia"`

### D9.3: BuildServerIntegration Plugin

**Source:** `src/plugins/BuildServerIntegration/`

- [ ] Port the settings page.
- [ ] Verify build status column shows in the revision grid (this requires the column provider integration from Task 3).
- [ ] **Commit:** `git commit -m "feat(plugin): port BuildServerIntegration to Avalonia"`

### D9.4: GitHub3 Plugin

**Source:** `src/plugins/GitHub3/`

- [ ] Port the PR creation dialog.
- [ ] Port the OAuth flow (opens browser for GitHub login).
- [ ] **Commit:** `git commit -m "feat(plugin): port GitHub3 PR integration to Avalonia"`

### D9.5: Statistics Plugin

**Source:** `src/plugins/Statistics/`

- [ ] Add `LiveChartsCore.SkiaSharp.Avalonia` to `Directory.Packages.props` and the plugin's csproj.
- [ ] Port the statistics charts view.
- [ ] **Commit:** `git commit -m "feat(plugin): port Statistics charts to Avalonia/LiveCharts"`

### D9.6: Remaining Plugins (CreateLocalBranches, FindLargeFiles, Gource, ProxySwitcher, ReleaseNotesGenerator, AutoCompileSubmodules)

For each:
- [ ] Port settings page UI to Avalonia
- [ ] Verify plugin loads and its action works
- [ ] Commit per plugin

---

## Plan D Done Criteria

- [ ] All dialogs in the design spec are implemented
- [ ] All menu items are wired to their dialogs
- [ ] All plugins load and function
- [ ] `dotnet build GitExtensions.Mac.slnx` — 0 errors
- [ ] `dotnet test GitExtensions.Mac.slnx` — all tests pass
- [ ] `./scripts/build-mac.sh` produces a working `.app` bundle
- [ ] `open artifacts/GitExtensions.app` opens the app and all features work
- [ ] GitHub Actions CI is green
- [ ] `grep -r "System.Windows.Forms" src/app/GitUI.Avalonia/` — zero results
- [ ] `docs/mac-port/TASK-STATUS.md` — all tasks marked `complete`

---

## Completion Checklist

When all four plans are done:

- [ ] The app runs natively on macOS (arm64 and x64)
- [ ] Full feature parity with the Windows version
- [ ] Settings persist between sessions (`~/.config/gitextensions/settings.json`)
- [ ] Credentials stored securely in macOS Keychain
- [ ] CI builds and publishes on every push to master
- [ ] `.dmg` distributable ready for GitHub Release
- [ ] `docs/mac-port/TASK-STATUS.md` fully complete
