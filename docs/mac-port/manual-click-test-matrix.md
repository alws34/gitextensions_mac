# Avalonia Mac Port Manual Click-Test Matrix

Last updated: 2026-05-04

Use this matrix to compare the Avalonia macOS port with original GitExtensions WinForms behavior. Run each test in the Mac port, and when practical run the same action in original GitExtensions against an equivalent repository. Mark `Pass` only when the Mac port exposes the same user-visible command, dialog, status update, repository mutation, and refresh behavior, with macOS-native substitutions such as Finder instead of Windows File Explorer.

Known or expected gaps should be marked `Fail` or `Gap`, not skipped. Do not use production repositories for destructive tests.

## Suggested Test Repositories

The matrix uses the prerequisite keys below. A tester may use an equivalent existing fixture, but the repo should contain local and remote branches, tags, dirty files, stashes, a submodule, and a worktree.

```bash
export GE_QA_ROOT="${TMPDIR:-/tmp}/ge-mac-click-matrix"
rm -rf "$GE_QA_ROOT"
mkdir -p "$GE_QA_ROOT"

git init --bare "$GE_QA_ROOT/origin.git"
git clone "$GE_QA_ROOT/origin.git" "$GE_QA_ROOT/main"
cd "$GE_QA_ROOT/main"
git config user.name "Mac QA"
git config user.email "mac-qa@example.com"

mkdir -p src docs
printf "line 1\nline 2\n" > src/app.txt
printf "# QA repo\n" > README.md
git add .
git commit -m "initial commit"
git push -u origin main

git init "$GE_QA_ROOT/submodule-source"
cd "$GE_QA_ROOT/submodule-source"
git config user.name "Mac QA"
git config user.email "mac-qa@example.com"
printf "submodule file\n" > sub.txt
git add sub.txt
git commit -m "submodule initial"

cd "$GE_QA_ROOT/main"
git -c protocol.file.allow=always submodule add "$GE_QA_ROOT/submodule-source" vendor/submodule
git commit -m "add submodule"
git tag v1.0

git checkout -b feature/diff-fixture
printf "feature line\n" >> src/app.txt
git add src/app.txt
git commit -m "feature change"
git push origin feature/diff-fixture

git checkout main
git checkout -b local-only
printf "local branch\n" > local.txt
git add local.txt
git commit -m "local only commit"
git checkout main
git push origin main

git worktree add "$GE_QA_ROOT/worktree-feature" -b worktree-branch main

git clone "$GE_QA_ROOT/origin.git" "$GE_QA_ROOT/other"
cd "$GE_QA_ROOT/other"
git config user.name "Mac QA"
git config user.email "mac-qa@example.com"
printf "remote-only\n" >> README.md
git add README.md
git commit -m "remote only commit"
git push origin main

cd "$GE_QA_ROOT/main"
git fetch origin
printf "stash me\n" >> README.md
printf "temporary\n" > temp-stash.txt
git stash push -m "qa stash one" --include-untracked
printf "staged change\n" >> src/app.txt
git add src/app.txt
printf "unstaged change\n" >> README.md
printf "new untracked\n" > new-untracked.txt

git clone "$GE_QA_ROOT/origin.git" "$GE_QA_ROOT/conflict-repo"
cd "$GE_QA_ROOT/conflict-repo"
git config user.name "Mac QA"
git config user.email "mac-qa@example.com"
git checkout -b conflict-left
printf "left\n" > conflict.txt
git add conflict.txt
git commit -m "left conflict"
git checkout main
git checkout -b conflict-right
printf "right\n" > conflict.txt
git add conflict.txt
git commit -m "right conflict"
git merge conflict-left || true

mkdir -p "$GE_QA_ROOT/empty"
cd "$GE_QA_ROOT/empty"
git init
git config user.name "Mac QA"
git config user.email "mac-qa@example.com"
```

| Key | Test repo setup prerequisite |
|---|---|
| P0 | App launched with no repository open; dashboard is visible. |
| P1 | Open `$GE_QA_ROOT/main`; allow revision grid and sidebar to finish loading. |
| P2 | Open `$GE_QA_ROOT/main` with staged, unstaged, and untracked files still present. |
| P3 | Open `$GE_QA_ROOT/main` after `git fetch origin`; repo has local, remote, ahead/behind, and tag refs. |
| P4 | Open `$GE_QA_ROOT/main`; bare `origin` is reachable at `$GE_QA_ROOT/origin.git`. |
| P5 | Open `$GE_QA_ROOT/main`; submodule `vendor/submodule` exists. |
| P6 | Open `$GE_QA_ROOT/main`; linked worktree `$GE_QA_ROOT/worktree-feature` exists. |
| P7 | Open `$GE_QA_ROOT/main`; stash `qa stash one` exists. |
| P8 | Open `$GE_QA_ROOT/main`; select a normal commit that changes at least one file. |
| P9 | Open `$GE_QA_ROOT/main`; select the artificial `Working directory` or `Index` row. |
| P10 | Open `$GE_QA_ROOT/conflict-repo`; merge conflict is in progress. |
| P11 | Open `$GE_QA_ROOT/empty`; repository has no commits. |

## Top Menus

| ID | Surface | Test repo setup prerequisite | Action | Expected result against original GitExtensions behavior | Pass/Fail |
|---|---|---|---|---|---|
| TM-001 | File menu | P0 | Open `File` menu. | Menu is reachable from macOS menu bar or in-window menu and contains repository start actions equivalent to original `Start`: open, clone, init/create, recent repositories, settings, close/dashboard, and exit/quit where platform-appropriate. |  |
| TM-002 | File > Open Repository | P0 | Click `Open Repository...` and choose `$GE_QA_ROOT/main`. | Repository opens, title changes to repo name, recent repositories update, toolbar/sidebar/grid become active, and dashboard hides. |  |
| TM-003 | File > Clone Repository | P0 | Click `Clone Repository...`. | Clone dialog opens, accepts a remote URL and target directory, and behaves like original clone flow without needing an already-open repo. |  |
| TM-004 | File > Init New Repository | P0 | Click `Init New Repository...` and choose a temporary folder. | Init dialog creates a valid git repo, then the app can open it and show the empty repository state. |  |
| TM-005 | File > Recent Repositories | P1 | Open `Recent Repositories` and choose `$GE_QA_ROOT/main`. | Recent list shows full paths like original dashboard/recent menu; choosing an entry switches to that repo and refreshes UI. |  |
| TM-006 | File > Settings | P1 | Click `Settings...`. | Settings window opens once, is not blocked by repo load, and includes Mac-port settings pages that correspond to original settings categories. |  |
| TM-007 | File > Close Repository | P1 | Click `Close Repository`. | App returns to dashboard/no-repo state like original `Close (go to Dashboard)`; repo-specific toolbar, grid, and status labels clear. |  |
| TM-008 | File > Exit/Quit | P0 | Open menu and verify quit item. | Quit/Exit is present in the correct macOS location or File menu and prompts/saves state consistently with original shutdown behavior. |  |
| TM-009 | Repository > Fetch | P4 | Click `Repository > Fetch`. | Fetch runs against configured remotes, progress/status is visible, remote refs refresh, and no modal stays stuck after completion. |  |
| TM-010 | Repository > Pull | P4 | Click `Repository > Pull...`. | Pull dialog opens with current branch/upstream defaults matching original; successful pull refreshes grid, branch selector, sidebar, and ahead/behind status. |  |
| TM-011 | Repository > Push | P4 | Click `Repository > Push...`. | Push dialog opens with current branch and remote defaults; successful push refreshes ahead/behind status. |  |
| TM-012 | Repository > Manage Remotes | P4 | Click `Manage Remotes...`. | Remotes dialog lists `origin`, allows add/edit/remove/save, and matches original remote-management outcomes. |  |
| TM-013 | Repository > Resolve Conflicts | P10 | Click `Resolve Conflicts...`. | Conflict resolution dialog opens only when relevant and shows conflicted files/actions comparable to original solve conflicts flow. |  |
| TM-014 | Repository > Clean Repository | P2 | Click `Clean Repository...`. | Cleanup dialog opens, previews/selects untracked/ignored cleanup safely, and requires confirmation before destructive changes. |  |
| TM-015 | Repository > Archive | P8 | Click `Archive...`. | Archive dialog opens for selected/current revision and can create an archive without changing working tree. |  |
| TM-016 | Repository > Verify | P1 | Click `Verify (fsck)...`. | Verify dialog/process runs `git fsck` equivalent and displays output/errors like original Git maintenance verification. |  |
| TM-017 | Repository > Submodules | P5 | Click `Submodules...`. | Submodules dialog lists `vendor/submodule` and exposes update/sync/add actions matching original manage submodules basics. |  |
| TM-018 | Repository > Add Submodule | P1 | Click `Add Submodule...`. | Add submodule dialog opens, validates URL/path, and after success sidebar and grid refresh. |  |
| TM-019 | Repository > Worktrees | P6 | Click `Worktrees...`. | Worktree dialog lists main and linked worktrees with branch/head data and allows safe management like original. |  |
| TM-020 | Repository > Create Worktree | P3 | Click `Create Worktree...`. | Create worktree dialog accepts path/branch/new-branch option; successful create updates worktree list/sidebar. |  |
| TM-021 | Repository > Compare to Branch | P3 | Click `Compare to Branch...`. | Compare dialog opens with local/remote branch choices and displays comparison/diff equivalent to original. |  |
| TM-022 | Repository > Reset Changes | P2 | Click `Reset Changes...`. | Reset dialog opens with changed files and confirmation before discarding changes. |  |
| TM-023 | Repository > Sparse Working Copy | P1 | Click `Sparse Working Copy...`. | Sparse working copy dialog opens or reports unsupported state clearly, without crashing. |  |
| TM-024 | Repository > MailMap | P1 | Click `MailMap...`. | Mailmap dialog opens and edits `.mailmap` behavior equivalent to original `Edit .mailmap`. |  |
| TM-025 | Repository > Refresh | P2 | Click `Refresh` or press F5. | Grid, sidebar counts, status bar, and diff/file panels refresh to match current git state. |  |
| TM-026 | Commands > Commit | P2 | Click `Commit...` or press Ctrl+Enter. | Commit dialog opens with unstaged/staged lists, diff preview, message box, amend, cancel, and commit controls. |  |
| TM-027 | Commands > Add Files | P2 | Click `Add Files...`. | Add files dialog opens and can add untracked files like original. |  |
| TM-028 | Commands > Add to .gitignore | P2 | Click `Add to .gitignore...`. | Dialog opens for ignore patterns; saving updates `.gitignore` and refreshes status. |  |
| TM-029 | Commands > Branch create/checkout/delete/rename | P3 | Open each branch command dialog. | Each dialog opens with current branch/ref defaults and mutates branch refs like original after confirmation. |  |
| TM-030 | Commands > Merge/Rebase | P3 | Open `Merge Branch...` and `Rebase...`. | Dialogs show branch choices, support standard options, start operation, and display in-progress/abort state when needed. |  |
| TM-031 | Commands > Interactive Rebase | P8 | Click `Interactive Rebase...`. | Interactive rebase dialog opens for the chosen base/current range and does not hard-code an unsafe default. |  |
| TM-032 | Commands > Tags | P8 | Open `Create Tag...` and `Delete Tag...`. | Tag dialogs list/select commits/tags and create/delete refs with confirmation where original does. |  |
| TM-033 | Commands > Stash | P7 | Click `Stash...`. | Stash dialog lists existing stashes, shows selected stash diff, and supports create/apply/pop/drop. |  |
| TM-034 | Commands > Cherry Pick/Revert | P8 | Open `Cherry Pick...` and `Revert Commit...`. | Dialogs target the selected commit when applicable; operations show progress and refresh grid/status afterward. |  |
| TM-035 | Commands > Patch | P8 | Open `Apply Patch...`, `Format Patch...`, and `View Patch...`. | Patch dialogs open, validate paths/revisions, and match original patch workflow outcomes. |  |
| TM-036 | Commands > Bisect/Reflog | P3 | Open `Bisect...` and `Reflog...`. | Dialogs open, show state/history, and allow the same basic actions as original. |  |
| TM-037 | Commands > Git Ignore/Attributes | P1 | Open `.gitignore...` and `.gitattributes...`. | Editors/dialogs open for the repo files and save changes without requiring Windows-only tools. |  |
| TM-038 | Commands > Delete Remote Branch | P4 | Click `Delete Remote Branch...`. | Dialog lists remote branches and confirms delete before pushing remote ref deletion. |  |
| TM-039 | Commands > View Diff/Git Log | P8 | Open `View Diff...` and `Git Log...`. | Diff/log dialogs open for current repo/selection and display output equivalent to original. |  |
| TM-040 | Commands > Go to Commit | P3 | Click `Go to Commit...`, enter a visible short hash. | Grid selects and scrolls to the matching commit; invalid hash gives a non-crashing message. |  |
| TM-041 | Tools > Open Terminal Here | P1 | Click `Open Terminal Here`. | macOS Terminal opens at repo working directory, equivalent to original shell button. |  |
| TM-042 | Tools > Open in Finder | P1 | Click `Open in Finder`. | Finder opens the repo directory, equivalent to original File Explorer action. |  |
| TM-043 | Tools > Scripts | P1 | Click `Scripts...`. | Scripts manager/dialog opens and script commands remain compatible with original user scripts where possible. |  |
| TM-044 | Tools > Git Hooks | P1 | Click `Git Hooks...`. | Hooks dialog opens for the repo and can view/edit hook files like original. |  |
| TM-045 | View > Toggle Left Panel | P1 | Click `Toggle Left Panel` twice. | Left panel hides and restores with previous width; splitter does not leave unusable gaps. |  |
| TM-046 | View > Graph toggles | P3 | Toggle `Show Stashes in Graph`, `Show Worktrees in Graph`, `Show Tags`, and `Show First Parent Only`. | Items show checked/unchecked state, persist like original, and grid reloads or clearly requires refresh when view setting changes. |  |
| TM-047 | Navigate menu | P3 | Use parent, child, back, forward, and go-to commands. | Selection moves like original revision grid navigation and never lands outside visible rows. |  |
| TM-048 | Help menu | P0 | Open updates, changelog, command-line help, and about. | Dialogs open without a repo, show current app/version data, and platform-specific links work. |  |
| TM-049 | Missing original menu groups | P1 | Compare top-level menus with original `Start`, `Dashboard`, `Repository hosts`, `Plugins`, `Tools`, `Help`. | Equivalent commands are present somewhere discoverable; any missing original command is recorded as a parity gap. |  |

## Toolbar Buttons

| ID | Surface | Test repo setup prerequisite | Action | Expected result against original GitExtensions behavior | Pass/Fail |
|---|---|---|---|---|---|
| TB-001 | Toolbar layout | P1 | Inspect toolbar at app launch and after resizing window. | Buttons are icon-first, aligned, clipped gracefully, and correspond to original Standard toolbar groups. |  |
| TB-002 | Toggle left panel | P1 | Click left-panel toggle. | Left panel collapses/expands and retains usable grid width, matching original toolbar toggle. |  |
| TB-003 | Refresh | P2 | Modify a file externally, then click refresh. | Status counts, artificial rows, file list, and sidebar working directory section update. |  |
| TB-004 | Working directory selector | P1 | Click working-directory button. | Recent repository flyout opens; choosing another repo switches the window and updates title/status. |  |
| TB-005 | Branch selector | P3 | Choose `local-only` or `feature/diff-fixture`. | Checkout starts, status bar reports progress/result, branch label and sidebar current branch update. |  |
| TB-006 | Commit button | P2 | Click Commit toolbar button. | Commit dialog opens exactly as menu/shortcut does. |  |
| TB-007 | Pull split button default | P4 | Click main pull split-button face. | Pull dialog opens with merge/default pull behavior comparable to original default pull action. |  |
| TB-008 | Pull split menu | P4 | Open pull flyout and click Pull + Merge, Pull + Rebase, Fetch All, Fetch pruning. | Each command runs the matching git action, shows progress, and refreshes refs/status; destructive rebase requires visible feedback. |  |
| TB-009 | Push button | P4 | Click Push. | Push dialog opens and uses current branch/upstream defaults. |  |
| TB-010 | Stash split button default | P7 | Click main stash split-button face. | Stash dialog opens, matching original manage stashes button. |  |
| TB-011 | Stash split menu | P7 | Open stash flyout and use Stash, Stash Staged, Pop Stash, Manage Stashes. | Each action updates stash list and working tree; failures show clear status instead of silent no-op. |  |
| TB-012 | Finder button | P1 | Click folder/Finder toolbar button. | Finder opens repo root. |  |
| TB-013 | Terminal button | P1 | Click terminal toolbar button. | Terminal opens at repo root. |  |
| TB-014 | Settings button | P1 | Click settings toolbar button. | Settings window opens with no duplicate modal lock. |  |
| TB-015 | Original submodule/worktree toolbar parity | P5, P6 | Compare with original toolbar submodule/worktree buttons. | Mac port exposes equivalent submodule and worktree entry points from toolbar or clearly discoverable menu; missing toolbar affordance is recorded as gap. |  |
| TB-016 | Toolbar disabled state | P0 | Inspect toolbar before opening a repo and click repo-specific controls. | Repo-specific commands are disabled or harmless no-ops, matching original no-repo behavior. |  |

## Sidebar Sections And Context Menus

| ID | Surface | Test repo setup prerequisite | Action | Expected result against original GitExtensions behavior | Pass/Fail |
|---|---|---|---|---|---|
| SB-001 | Sidebar sections | P1 | Inspect left panel. | Sections for Working Directory, Branches, Remotes, Tags, Submodules, Worktrees, and Stashes are visible or discoverable like original left panel roots. |  |
| SB-002 | Collapse all | P1 | Click collapse-all button. | Expanded sidebar roots collapse; user can reopen each section. |  |
| SB-003 | Search box | P3 | Search for `feature`, `origin`, and `v1.0`. | Branch, remote, and tag lists filter case-insensitively without clearing current selection unexpectedly. |  |
| SB-004 | Working Directory count | P2 | Inspect Working Directory header. | Header shows dirty-file count equivalent to original status visualizer; file status letters/colors match staged/unstaged/untracked state. |  |
| SB-005 | Working Directory refresh | P2 | Stage/unstage a file outside the app, then refresh. | Working Directory list and status bar update to reflect current git status. |  |
| SB-006 | Local branch current marker | P3 | Inspect `main` or current branch in Branches. | Current branch is visually distinct, equivalent to original bold/current branch indicator. |  |
| SB-007 | Local branch double-click checkout | P3 | Double-click `local-only` or another safe branch. | Branch checkout occurs, grid reloads, status bar reports new branch, and dirty-change conflicts are handled with a warning. |  |
| SB-008 | Branch context menu opens | P3 | Right-click a local branch. | Context menu opens only when a branch is selected and includes checkout, merge, create, rename, delete, push, and reset-to-remote equivalents. |  |
| SB-009 | Branch context > Checkout | P3 | Right-click branch and choose Checkout. | Same result as double-click checkout. |  |
| SB-010 | Branch context > Merge into current | P3 | Choose Merge into current for a non-current branch. | Merge runs or opens confirmation/progress; conflict state shows action banner like original. |  |
| SB-011 | Branch context > Create Branch from here | P3 | Choose Create Branch from here. | Create Branch dialog opens with selected branch/ref as base, not an unrelated default. |  |
| SB-012 | Branch context > Rename Branch | P3 | Rename a disposable branch. | Rename dialog opens, validates name, and branch list refreshes after success. |  |
| SB-013 | Branch context > Delete Branch | P3 | Delete a disposable fully merged branch. | Delete requires confirmation or safe failure, removes branch, and refreshes list. |  |
| SB-014 | Branch context > Push | P4 | Push a local branch. | Push uses selected branch and configured remote/upstream, with visible progress/errors. |  |
| SB-015 | Branch context > Reset to Remote | P3 | Choose Reset to Remote on a branch with upstream. | Operation requires confirmation before hard reset and refreshes refs/status after success. |  |
| SB-016 | Remote branch list | P3 | Expand Remotes. | Remote refs display using original-style hierarchy/name clarity, including `origin/feature/diff-fixture`. |  |
| SB-017 | Remote context > Fetch | P4 | Right-click remote branch and choose Fetch. | Fetch targets the remote name, refreshes remote refs, and handles network errors visibly. |  |
| SB-018 | Remote context > Checkout as local | P3 | Choose Checkout as New Local Branch for `origin/feature/diff-fixture`. | New local branch is created/tracking remote or failure is explained; branch selector/sidebar refresh. |  |
| SB-019 | Remote context > Delete Remote Branch | P4 | Choose Delete Remote Branch. | Delete remote branch dialog opens with selected remote branch preselected where possible. |  |
| SB-020 | Tags section | P3 | Expand Tags. | Tag list shows `v1.0`, sorted and searchable like original. |  |
| SB-021 | Tag context menu | P3 | Right-click `v1.0`; create branch, then delete a disposable tag. | Create Branch at Tag and Delete Tag target the selected tag and refresh refs. |  |
| SB-022 | Stashes section | P7 | Expand Stashes and select `qa stash one`. | Stash list shows summary/order equivalent to `git stash list`. |  |
| SB-023 | Stash double-click | P7 | Double-click a stash. | Behavior matches original left-panel stash default action; if pop/apply happens, working tree and stash list refresh with visible status. |  |
| SB-024 | Stash context menu | P7 | Right-click stash and choose Pop, Apply, Drop on disposable stashes. | Pop applies and removes; Apply keeps stash; Drop confirms/removes; all refresh sidebar/status. |  |
| SB-025 | Submodules section | P5 | Expand Submodules. | `vendor/submodule` appears with status icon/tooltip equivalent to original submodule status indicators. |  |
| SB-026 | Submodule double-click/open | P5 | Double-click submodule or choose Open Submodule. | App opens submodule repository path and updates title/sidebar/grid. |  |
| SB-027 | Submodule context > Update | P5 | Choose Update. | Runs `submodule update --init` for selected submodule and shows result/error. |  |
| SB-028 | Worktrees section | P6 | Expand Worktrees. | Linked worktree path appears; original worktree panel behavior is represented clearly. |  |
| SB-029 | Worktree double-click/open | P6 | Double-click `$GE_QA_ROOT/worktree-feature`. | App switches to that worktree repo, updates branch label and title. |  |
| SB-030 | Worktree context parity | P6 | Right-click a worktree entry. | Context menu should expose original-equivalent open/manage/remove actions or record missing context menu as gap. |  |
| SB-031 | Sidebar selection -> grid filter parity | P3 | Select a branch/tag/root in sidebar. | If original filters/highlights the revision grid for selected refs, Mac port should match or record parity gap. |  |
| SB-032 | Sidebar persistence | P1 | Collapse/expand roots, close and reopen app. | Root visibility/order/expanded state persists like original settings where supported. |  |

## Revision Grid And Context Menu

| ID | Surface | Test repo setup prerequisite | Action | Expected result against original GitExtensions behavior | Pass/Fail |
|---|---|---|---|---|---|
| RG-001 | Grid load | P1 | Open repo and wait for grid. | Artificial rows plus commit history load without blank area; max row cap does not hide fixture commits. |  |
| RG-002 | Graph rendering | P3 | Inspect merge/branch lane graph. | Colored graph lanes, branch splits/joins, and row alignment match original visual behavior. |  |
| RG-003 | Column headers | P1 | Inspect headers. | Graph, Subject, Author, Date, and Hash columns are visible; missing original columns such as avatar/build/notes are recorded as gaps. |  |
| RG-004 | Column resize | P1 | Drag each header splitter. | Row cells follow header width, no text overlap, and width remains usable after refresh. |  |
| RG-005 | Ref badges | P3 | Inspect commits with branch/tag refs. | Local branches, remote branches, tags, and HEAD/current markers use distinguishable badge colors like original. |  |
| RG-006 | Current revision styling | P3 | Locate current HEAD row. | Current revision is visually marked and can be navigated to like original. |  |
| RG-007 | Select revision | P8 | Click a normal commit. | Status bar shows hash; commit summary, file list, tree, and diff panel update to selected revision. |  |
| RG-008 | Artificial rows | P9 | Select `Working directory` and `Index`. | Details show working/index changes like original artificial commits; unavailable commit-only actions are disabled. |  |
| RG-009 | Grid keyboard navigation | P3 | Use arrow keys/page keys if supported. | Selection moves predictably, details follow selection, and row stays scrolled into view. |  |
| RG-010 | Quick filter bar | P3 | Press Ctrl+F, filter by message, author, hash, all. | Filter applies to grid with same semantics as original filter toolbar or records missing modes/gaps. |  |
| RG-011 | View toggles apply | P3 | Toggle show tags/stashes/worktrees/first parent and refresh. | Grid contents and ref badges reflect toggles like original. |  |
| RG-012 | Context menu opens | P8 | Right-click a normal commit row. | Context menu opens with commit actions and no accidental selection loss. |  |
| RG-013 | Context > Checkout this commit | P8 | Choose Checkout this commit on disposable repo. | Detached HEAD checkout occurs only after expected confirmation/status; branch selector/status update. |  |
| RG-014 | Context > Cherry-pick | P8 | Choose Cherry-pick on non-HEAD commit. | Cherry-pick dialog/action targets selected commit and shows success/conflict state. |  |
| RG-015 | Context > Revert | P8 | Choose Revert. | Revert dialog/action targets selected commit and refreshes grid/status after operation. |  |
| RG-016 | Context > Create Branch here | P8 | Choose Create Branch here. | Create Branch dialog uses selected commit as base; created branch appears in sidebar/grid. |  |
| RG-017 | Context > Create Tag here | P8 | Choose Create Tag here. | Create Tag dialog uses selected commit; tag appears as ref badge and in Tags sidebar. |  |
| RG-018 | Context > Reset hard | P8 | Choose Reset to this (hard). | Destructive reset requires confirmation and refreshes working tree/status like original. |  |
| RG-019 | Context > Interactive Rebase | P8 | Choose Interactive Rebase from here. | Dialog/action targets selected commit and exposes continue/skip/abort state when relevant. |  |
| RG-020 | Context > Copy hash | P8 | Choose Copy commit hash and paste elsewhere. | Clipboard receives the selected commit hash; original supports short/full variants, so missing variants are recorded as gaps. |  |
| RG-021 | Context menu on empty area | P1 | Right-click blank grid space. | Menu is suppressed or disabled without throwing, like original. |  |
| RG-022 | Context menu on artificial row | P9 | Right-click Working directory/Index. | Commit-only actions are hidden/disabled; file/status actions remain available where original supports them. |  |
| RG-023 | Ref label context menu | P3 | Right-click a branch/tag/stash badge in the subject column. | Original-equivalent local branch, remote branch, tag, and stash context menus appear or missing support is recorded as gap. |  |
| RG-024 | Navigate parent/child | P3 | Use Navigate menu parent/child commands. | Selection moves to graph parent/child, including merge commits, matching original revision grid. |  |
| RG-025 | Go to commit | P3 | Enter full and short hashes. | Matching row is selected and scrolled into view; invalid input does not change selection silently. |  |
| RG-026 | Large history responsiveness | P1 | Scroll rapidly through grid. | UI remains responsive, graph rows render without tearing/blanking, and selection remains stable. |  |

## File List Context Menu

| ID | Surface | Test repo setup prerequisite | Action | Expected result against original GitExtensions behavior | Pass/Fail |
|---|---|---|---|---|---|
| FL-001 | Files tab load | P8 | Select a commit and open Files tab. | Changed files display in a tree with status icons/colors equivalent to original file status list. |  |
| FL-002 | File selection diff | P8 | Select a file in Files tab. | Diff panel updates to the selected file and preserves selected revision context. |  |
| FL-003 | File tree folders | P8 | Select nested path/folder rows if present. | Tree expands/collapses folders without selecting invalid file actions. |  |
| FL-004 | Context menu opens | P8 | Right-click a file. | Menu opens only for valid file selection and disables actions for folder/empty selection. |  |
| FL-005 | Context > Open Diff | P8 | Choose Open Diff. | Diff is shown for the selected file; if already shown, no duplicate or crash occurs. |  |
| FL-006 | Context > Open File | P2 | Right-click a working-tree file and choose Open File. | Default macOS app opens working file; action is disabled or clear for historical-only files. |  |
| FL-007 | Context > Reveal in Finder | P2 | Choose Reveal in Finder. | Finder reveals selected working file or parent folder, matching original Show in folder behavior. |  |
| FL-008 | Context > Open Containing Folder | P2 | Choose Open Containing Folder. | Finder opens containing directory. |  |
| FL-009 | Context > Copy File Path | P8 | Choose Copy File Path and paste. | Clipboard contains repo-relative path using POSIX separators. |  |
| FL-010 | Context > Copy Full Path | P2 | Choose Copy Full Path and paste. | Clipboard contains absolute macOS path for working-tree file. |  |
| FL-011 | Context > Copy File Name | P8 | Choose Copy File Name and paste. | Clipboard contains only basename. |  |
| FL-012 | Context > File History | P8 | Choose File History. | File history dialog opens for selected path and revision context. |  |
| FL-013 | Context > Blame | P8 | Choose Blame. | Blame dialog opens for selected path and revision context. |  |
| FL-014 | Original stage/unstage/reset parity | P2 | Compare file context menu with original. | Stage, unstage, reset selected file, interactive add/reset chunk, open with difftool, delete, move, add to ignore, skip-worktree, assume-unchanged, and script actions are present or recorded as gaps. |  |
| FL-015 | Original tree context parity | P8 | Right-click folder/tree background. | Expand all, collapse all, collapse root folders, and select all are present or recorded as gaps. |  |
| FL-016 | Submodule file context parity | P5 | Select submodule entry in file list if present. | Original-equivalent update, reset, stash, and commit submodule actions are available or recorded as gaps. |  |
| FL-017 | Multi-select parity | P8 | Attempt multi-select in file list. | Multi-select actions match original or missing multi-select is recorded as gap. |  |

## Diff View Toggles And Commit Details

| ID | Surface | Test repo setup prerequisite | Action | Expected result against original GitExtensions behavior | Pass/Fail |
|---|---|---|---|---|---|
| DV-001 | Commit summary | P8 | Select a normal commit. | Summary shows short hash, author, date, subject, body, and refs comparable to original Commit tab. |  |
| DV-002 | Details tabs | P8 | Switch between Diff, Files, and Tree tabs. | Tabs load lazily without losing selected revision; Tree tab shows repository tree for selected commit. |  |
| DV-003 | Unified diff toggle | P8 | Click Unified. | Unified diff is visible, line numbers remain aligned, and added/removed lines are colored like original. |  |
| DV-004 | Split diff toggle | P8 | Click Split. | Side-by-side/split view appears, columns resize, and content maps left/right correctly. |  |
| DV-005 | Toggle persistence | P8 | Switch to Split, select another file/commit, then return. | Selected diff mode remains active or follows original persistence setting. |  |
| DV-006 | Syntax highlighting | P8 | Select files with known extensions. | Diff/file syntax highlighting is applied where available without hiding diff coloring. |  |
| DV-007 | Horizontal scrolling | P8 | Select a file with long lines. | Horizontal scrollbars work and do not wrap unexpectedly when WordWrap is off. |  |
| DV-008 | Empty diff handling | P8 | Select a commit/file with no diff for current mode if available. | Diff area shows a clear empty state rather than stale diff from previous selection. |  |
| DV-009 | Original diff toolbar parity | P8 | Compare with original diff/file-list toolbar. | Original modes such as file tree/flat grouping, filter, show all parents, whitespace/options, and external difftool access exist or are recorded as gaps. |  |
| DV-010 | Action banner | P10 | Open repo with merge conflict. | In-progress action banner appears with accurate operation text and abort/reset button like original action bars. |  |
| DV-011 | Abort action | P10 | Click Abort Merge or equivalent on disposable conflict repo. | Operation aborts, banner hides, grid/status/sidebar refresh, and failures are visible. |  |

## Commit Dialog Basics

| ID | Surface | Test repo setup prerequisite | Action | Expected result against original GitExtensions behavior | Pass/Fail |
|---|---|---|---|---|---|
| CD-001 | Open commit dialog | P2 | Open Commit from menu, toolbar, and Ctrl+Enter. | Same dialog opens from all entry points with no duplicates. |  |
| CD-002 | Unstaged list | P2 | Inspect Unstaged list. | Modified/untracked unstaged files appear with names/status clarity equivalent to original. |  |
| CD-003 | Staged list | P2 | Inspect Staged list. | Staged files appear separately from unstaged files. |  |
| CD-004 | Diff preview selection | P2 | Select unstaged then staged files. | Diff preview switches between working-tree and cached diff, with colored added/removed lines. |  |
| CD-005 | Stage all | P2 | Click Stage All. | All unstaged/untracked files move to Staged; status refreshes without closing dialog. |  |
| CD-006 | Unstage all | P2 | Click Unstage All. | All staged files move to Unstaged; diff preview clears or refreshes. |  |
| CD-007 | Per-file stage | P2 | Double-click/right-click an unstaged file and stage it. | Only selected file moves to Staged, matching original per-file staging. |  |
| CD-008 | Per-file unstage | P2 | Double-click/right-click a staged file and unstage it. | Only selected file moves to Unstaged. |  |
| CD-009 | Empty message validation | P2 | Click Commit with blank message. | Commit is blocked with visible validation; no git commit is created. |  |
| CD-010 | Basic commit | P2 | Stage a disposable file, enter message, click Commit. | Git commit is created, dialog closes, grid/sidebar/status refresh, and working tree reflects remaining changes. |  |
| CD-011 | Amend checkbox | P2 | Check Amend last commit. | Last commit message loads into editor; amend commit updates HEAD like original after confirmation/commit. |  |
| CD-012 | Cancel | P2 | Make staging changes, click Cancel. | Dialog closes; git index changes already performed remain, matching original explicit staging behavior. |  |
| CD-013 | Commit dialog original parity | P2 | Compare controls with original FormCommit. | Missing original basics such as sign-off, author/date, commit templates, spellcheck/autocomplete, GPG/signing, amend/no-verify options, and hunk staging are recorded as gaps. |  |

## Stash, Branch, Remote, Submodule, And Worktree Flows

| ID | Surface | Test repo setup prerequisite | Action | Expected result against original GitExtensions behavior | Pass/Fail |
|---|---|---|---|---|---|
| WF-001 | Stash dialog list | P7 | Open Stash dialog. | Existing stashes list with newest first and selected stash diff preview. |  |
| WF-002 | Stash with message | P2 | Enter `manual qa stash` and click Stash. | Working changes are stashed with message, list refreshes, and working tree/index state matches original stash behavior. |  |
| WF-003 | Stash apply | P7 | Select stash and click Apply. | Stash applies but remains in list; conflicts/status are visible. |  |
| WF-004 | Stash pop | P7 | Select disposable stash and click Pop. | Stash applies and is removed; list and working directory refresh. |  |
| WF-005 | Stash drop | P7 | Select disposable stash and click Drop. | Confirmation appears if original requires it; stash is removed and list refreshes. |  |
| WF-006 | Stash staged | P2 | Stage one file, use toolbar Stash Staged. | Only staged changes are stashed where git supports it; unstaged changes remain. |  |
| WF-007 | Branch create | P3 | Create `qa/new-branch` from `main`, with checkout checked. | New branch is created, checked out, and visible in toolbar/sidebar/grid refs. |  |
| WF-008 | Branch create without checkout | P3 | Create disposable branch with checkout unchecked. | Branch appears but current branch remains unchanged. |  |
| WF-009 | Branch checkout existing | P3 | Checkout `feature/diff-fixture`. | Current branch changes, grid/status/sidebar refresh, and dirty conflict warnings match original. |  |
| WF-010 | Branch checkout as new branch from remote | P3 | Use remote context or checkout dialog to create local from `origin/feature/diff-fixture`. | Local tracking branch is created or clear validation explains duplicate name. |  |
| WF-011 | Branch rename | P3 | Rename disposable branch. | Rename validates collisions and refreshes all branch selectors. |  |
| WF-012 | Branch delete | P3 | Delete disposable fully merged branch. | Delete confirmation/safety matches original and branch disappears. |  |
| WF-013 | Branch merge conflict flow | P10 | Start/inspect merge conflict flow. | Conflict state appears in action banner; Resolve Conflicts and Abort actions are discoverable. |  |
| WF-014 | Rebase flow | P3 | Start a simple rebase from dialog on disposable branch. | Progress/status and continue/skip/abort behavior match original or missing controls are recorded as gaps. |  |
| WF-015 | Remote add | P4 | In Remotes dialog, add remote `backup` pointing to `$GE_QA_ROOT/origin.git`. | Remote appears in dialog/sidebar after save; duplicate names validate. |  |
| WF-016 | Remote edit | P4 | Edit `backup` URL or name. | Remote config changes persist and list refreshes. |  |
| WF-017 | Remote remove | P4 | Remove `backup`. | Remote is removed after confirmation/safe action; refs/sidebar refresh. |  |
| WF-018 | Fetch remote branch | P4 | Fetch from origin using menu/sidebar/toolbar. | Remote branch refs update without changing working tree. |  |
| WF-019 | Push local branch | P4 | Push `local-only` or another disposable branch. | Push dialog/process creates remote branch or reports upstream prompt like original. |  |
| WF-020 | Delete remote branch | P4 | Delete a disposable remote branch. | Remote branch is deleted after confirmation and disappears after fetch/refresh. |  |
| WF-021 | Submodules dialog list | P5 | Open Submodules dialog. | Existing submodule paths list correctly. |  |
| WF-022 | Submodule update all | P5 | Click Update All. | Runs recursive update, shows progress/output, and updates sidebar status. |  |
| WF-023 | Submodule sync | P5 | Click Sync. | Sync command runs with visible output/status and no UI freeze. |  |
| WF-024 | Add submodule | P1 | Add a disposable submodule from `$GE_QA_ROOT/submodule-source`. | Dialog validates URL/path, updates `.gitmodules`, stages/commits state like original. |  |
| WF-025 | Open submodule as repo | P5 | Open submodule from sidebar. | Main window switches to submodule repo and can navigate back via recent repos. |  |
| WF-026 | Worktree dialog list | P6 | Open Worktrees dialog. | Main and linked worktrees list with path, branch, and short HEAD. |  |
| WF-027 | Create worktree existing branch | P3 | Create disposable worktree from existing branch. | Worktree path is created, dialog closes or reports success, sidebar list refreshes. |  |
| WF-028 | Create worktree new branch | P3 | Check `Create new branch (-b)` and create branch/worktree. | New branch and worktree are created together; branch appears in sidebar. |  |
| WF-029 | Remove worktree | P6 | Select disposable worktree and click Remove. | Removal requires safe confirmation or clearly reports failure for dirty/non-removable worktree. |  |
| WF-030 | Worktree switch | P6 | Double-click linked worktree in sidebar. | App opens selected worktree path and all repo-specific UI refreshes to that branch. |  |

## Recording Guidance

Use the `Pass/Fail` column as:

| Value | Meaning |
|---|---|
| Pass | Mac port matches original user-visible behavior for the tested path. |
| Fail | Command exists but behavior, target, refresh, state, or safety differs. |
| Gap | Original behavior is missing or not discoverable in the Mac port. |
| Blocked | Could not run because prerequisite setup, build, or app launch failed. |
| N/A | Platform-specific original behavior intentionally has no Mac equivalent. |

When marking `Fail` or `Gap`, capture: app build/commit, macOS version, fixture key, exact command clicked, selected repo/ref/file, expected original behavior, actual Mac behavior, screenshots if visual, and terminal output when the command mutates git state.
