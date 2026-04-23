// This file replaces CommonAssemblyInfo.cs (excluded in the .csproj) for this project.
// We intentionally omit [assembly: SupportedOSPlatform("windows7.0")] so that the NUnit
// engine does not mark this test assembly as Windows-only and skip it on macOS.
// CA1416 warnings from calling GitUI.Avalonia APIs (which carry that attribute) are
// suppressed via <NoWarn> in the project file.
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Git Extensions")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Git Extensions")]
[assembly: AssemblyProduct("Git Extensions")]
[assembly: AssemblyCopyright("Copyright \u00a9 2008-2025 Git Extensions Team")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("33.33.33")]
[assembly: AssemblyFileVersion("33.33.33")]
[assembly: AssemblyInformationalVersion("33.33.33")]
[assembly: CLSCompliant(isCompliant: false)]
[assembly: ComVisible(false)]
