# Building and Running on macOS

## Prerequisites

- **.NET 10 SDK** — install from [dot.net](https://dot.net) or via `brew install --cask dotnet-sdk`
- **Git** — `brew install git` if not already installed
- Submodules populated — run once if needed:
  ```bash
  git submodule update --init --recursive
  ```

If `dotnet` is not on your PATH, add it:
```bash
export PATH="$HOME/.dotnet:$PATH"
```

## Build

Use the Mac-specific solution file (`GitExtensions.Mac.slnx`), which excludes Windows-only projects:

```bash
dotnet build GitExtensions.Mac.slnx
```

## Run

```bash
dotnet run --project src/app/GitUI.Avalonia/GitUI.Avalonia.csproj
```

Or run the built binary directly after building:

```bash
./artifacts/Debug/bin/GitUI.Avalonia/GitUI.Avalonia
```

## Run Tests

```bash
dotnet test GitExtensions.Mac.slnx
```

## Notes

- Target framework: `net10.0`
- Entry point: `src/app/GitUI.Avalonia/` (Avalonia UI, cross-platform)
- The main `GitExtensions.slnx` is Windows-only — always use `GitExtensions.Mac.slnx` on macOS
- BugReporter, WinForms GitUI, and several plugins are excluded from the Mac solution
