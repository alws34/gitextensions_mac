# Task 0 — Fork & Build System

**Status:** pending  
**Depends on:** nothing  
**Blocks:** Task 1 (and everything else)  
**Estimated complexity:** Low — pure config changes, no logic

---

## Goal

Create the `gitextensions-mac` fork and make it compile on macOS with `dotnet build`. No Avalonia UI yet — just get a clean build with Windows-specific things removed.

---

## Steps

### 1. Fork the repo

On GitHub: fork `gitextensions/gitextensions` → `gitextensions-mac` (or your namespace).

Clone locally:
```bash
git clone https://github.com/<you>/gitextensions-mac.git
cd gitextensions-mac
```

### 2. Change the target framework

Edit `eng/RepoLayout.props`:
```xml
<!-- Change this line -->
<SolutionTargetFramework>net10.0-windows</SolutionTargetFramework>
<!-- To -->
<SolutionTargetFramework>net10.0</SolutionTargetFramework>
```

### 3. Remove WinForms from Directory.Build.props

Edit `Directory.Build.props`, remove:
```xml
<UseWindowsForms>true</UseWindowsForms>
```

### 4. Create the Mac solution file

Copy `GitExtensions.slnx` → `GitExtensions.Mac.slnx`.

Remove these projects from the Mac solution file (they are Windows-only):
- `setup/` — WiX installer projects
- `src/native/GitExtensionsShellEx/` — Windows Explorer shell extension
- `src/app/BugReporter/` — Windows-specific crash reporter (if it has WinForms dependencies)

Keep:
- `src/app/GitCommands/`
- `src/app/GitExtensions.Extensibility/`
- `src/app/GitExtUtils/`
- `src/app/ResourceManager/`
- All plugin projects (they will be ported in Task 17)
- `tests/` (unit tests — they should be cross-platform)

### 5. Delete Windows-only native code

```bash
rm -rf src/native/GitExtensionsShellEx/
```

Keep `src/native/GitExtSshAskPass/` — audit it (it may work on Mac or be simple to port).

### 6. Update package references

Edit `Directory.Packages.props` — see `docs/mac-port/PACKAGE-REPLACEMENTS.md` for the exact list.

Remove Windows-only packages. Add Avalonia packages:
```xml
<PackageVersion Include="Avalonia" Version="11.2.3" />
<PackageVersion Include="Avalonia.Desktop" Version="11.2.3" />
<PackageVersion Include="Avalonia.Themes.Fluent" Version="11.2.3" />
<PackageVersion Include="Avalonia.ReactiveUI" Version="11.2.3" />
<PackageVersion Include="AvaloniaEdit" Version="11.1.0" />
<PackageVersion Include="MsBox.Avalonia" Version="3.1.0" />
```

### 7. Add stub project for GitUI.Avalonia

Create `src/app/GitUI.Avalonia/GitUI.Avalonia.csproj` with minimal content so the solution compiles:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia" />
    <PackageReference Include="Avalonia.Desktop" />
    <PackageReference Include="Avalonia.Themes.Fluent" />
    <PackageReference Include="Avalonia.ReactiveUI" />
    <PackageReference Include="AvaloniaEdit" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../GitCommands/GitCommands.csproj" />
    <ProjectReference Include="../GitExtensions.Extensibility/GitExtensions.Extensibility.csproj" />
  </ItemGroup>
</Project>
```

Add a stub `Program.cs` so it compiles:
```csharp
// TODO: real entry point added in Task 1
Console.WriteLine("GitExtensions Mac - build OK");
```

Add this project to `GitExtensions.Mac.slnx`.

### 8. Fix Tier 2 compilation errors

The build will fail on Windows-specific APIs in `GitCommands`. Fix each:

**`GitCommands/Settings/AppSettings.cs`** — comment out or `#if` any Registry calls:
```csharp
#if !MACOS
// Registry-based code
#endif
```
Or better: extract to an interface `ISettingsBackend` and provide a no-op stub for now. Task 1 will provide the real Mac implementation.

**`GitCommands/Git/Extensions/ProcessExtensions.cs`** — remove Windows-specific process API calls, replace with cross-platform equivalents.

**`GitCommands/DiffMergeTools/VsDiffMerge.cs`** — add `[SupportedOSPlatform("windows")]` attribute or stub the class body with `throw new PlatformNotSupportedException()`.

**`GitCommands/ServiceContainerRegistry.cs`** — remove any Windows-specific service registrations, guard with `OperatingSystem.IsWindows()`.

### 9. Verify build

```bash
dotnet build GitExtensions.Mac.slnx
```

Fix any remaining compilation errors until the build is clean.

### 10. Verify tests still run

```bash
dotnet test GitExtensions.Mac.slnx --filter "Category!=Windows"
```

---

## Done Criteria

- [ ] `dotnet build GitExtensions.Mac.slnx` succeeds on macOS with 0 errors
- [ ] No WinForms package references remain in any project in the Mac solution
- [ ] `src/native/GitExtensionsShellEx/` is deleted
- [ ] `docs/mac-port/TASK-STATUS.md` updated to `complete` for Task 0

---

## Notes for next agent (Task 1)

After Task 0 completes, the repo compiles but does nothing. Task 1 builds the Avalonia infrastructure that all screen-port agents build on. See `AGENT-GUIDE.md` and the main design spec.
