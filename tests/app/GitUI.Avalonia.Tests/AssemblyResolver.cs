// tests/app/GitUI.Avalonia.Tests/AssemblyResolver.cs
// Pre-loads key test-infrastructure assemblies into the default AssemblyLoadContext so that
// the NUnit engine (which creates an isolated ALC when reflecting over the test assembly)
// can resolve them. NUnit3TestAdapter 5.x on net10 generates SelfRegisteredExtensions/
// TestingPlatformEntryPoint types that reference Microsoft.Testing.Platform; without this
// pre-load, the NUnit engine's private ALC cannot find those assemblies and reports
// "NUnit failed to load".
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace GitUI.Avalonia.Tests;

internal static class AssemblyResolver
{
    private static readonly string OutputDir =
        Path.GetDirectoryName(typeof(AssemblyResolver).Assembly.Location)!;

    // These assemblies are referenced by the auto-generated SelfRegisteredExtensions type and
    // must be pre-loaded into the default ALC before the NUnit engine reflects over the module.
    private static readonly string[] PreloadAssemblies =
    [
        "Microsoft.Testing.Platform",
        "Microsoft.Testing.Extensions.VSTestBridge",
        "Microsoft.Testing.Extensions.Telemetry",
        "Microsoft.Testing.Extensions.TrxReport.Abstractions",
        "Microsoft.Testing.Extensions.MSBuild",
        "NUnit3.TestAdapter",
    ];

    [ModuleInitializer]
    internal static void Register()
    {
        foreach (var name in PreloadAssemblies)
        {
            var path = Path.Combine(OutputDir, name + ".dll");
            if (File.Exists(path))
            {
                AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            }
        }

        // Hook the default ALC resolver so that assemblies copied to the output directory
        // are found by the NUnit engine's private ALC without a full probe.
        AssemblyLoadContext.Default.Resolving += (ctx, name) =>
        {
            var path = Path.Combine(OutputDir, name.Name + ".dll");
            return File.Exists(path) ? ctx.LoadFromAssemblyPath(path) : null;
        };
    }
}
