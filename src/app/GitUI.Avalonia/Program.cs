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

AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    string msg = $"[FATAL] UnhandledException: {e.ExceptionObject}";
    Console.Error.WriteLine(msg);
    File.AppendAllText("/tmp/ge_crash.log", msg + Environment.NewLine);
};

TaskScheduler.UnobservedTaskException += (_, e) =>
{
    string msg = $"[WARN] UnobservedTaskException: {e.Exception}";
    Console.Error.WriteLine(msg);
    File.AppendAllText("/tmp/ge_crash.log", msg + Environment.NewLine);
    e.SetObserved();
};

AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .WithInterFont()
    .LogToTrace()
    .StartWithClassicDesktopLifetime(args);
