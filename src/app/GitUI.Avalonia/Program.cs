using Avalonia;
using GitUI.Avalonia;

if (OperatingSystem.IsMacOS())
{
    // .app bundles don't inherit the shell PATH — ensure common git locations are reachable
    var extraPaths = new[] { "/opt/homebrew/bin", "/usr/local/bin", "/usr/bin", "/bin" };
    var current = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
    var combined = string.Join(":", extraPaths.Where(p => !current.Split(':').Contains(p)).Concat(current.Split(':')));
    Environment.SetEnvironmentVariable("PATH", combined);
}

AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .WithInterFont()
    .LogToTrace()
    .StartWithClassicDesktopLifetime(args);
