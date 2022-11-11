using System.Diagnostics;

using Arroyo;

using NetUpgradePlanner.Analysis;

Console.Clear();
Console.WriteLine("Enumerating files...");

var stopwatch = Stopwatch.StartNew();
var rootDirectory = @"C:\Program Files\Microsoft Visual Studio\2022\Preview";
var files = Directory.GetFiles(rootDirectory, "*", SearchOption.AllDirectories)
                     .Where(p => Path.GetExtension(p).ToLowerInvariant() is ".dll" or ".exe")
                     .ToArray();
Console.WriteLine($"Finished in {stopwatch.Elapsed}. Found {files.Length:N0} files.");

Console.WriteLine("Creating assembly set...");
stopwatch.Restart();
var assemblySet = ConsoleReporter.Run(pm => AssemblySet.Create(files, pm));
Console.WriteLine($"Finished in {stopwatch.Elapsed}. Found {assemblySet.Entries.Count:N0} assemblies.");
Console.WriteLine($"Peak working set was {Process.GetCurrentProcess().PeakWorkingSet64:N0}");
Console.WriteLine();

Console.WriteLine($"Exceptions thrown:");
Console.WriteLine($"File              = {GlobalStats.ExceptionsFile:N0}");
Console.WriteLine($"Custom Attributes = {GlobalStats.ExceptionsCustomAttribute:N0}");
Console.WriteLine($"IL                = {GlobalStats.ExceptionsIL:N0}");
Console.WriteLine($"----------------------------------");
Console.WriteLine($"Total             = {GlobalStats.Exceptions:N0}");

foreach (var (ex, count) in GlobalStats.ExceptionsAndCounts.OrderByDescending(kv => kv.Value))
{
    Console.WriteLine();
    Console.WriteLine($"Exception {count:N0}");
    Console.WriteLine(ex);
    Console.WriteLine();
}

internal sealed class ConsoleReporter : IProgressMonitor, IDisposable
{
    private DateTime _lastOutput;
    private readonly object _lock = new();

    private ConsoleReporter()
    {
        Console.Write("\x1B[s"); // Save cursor position
    }

    public void Dispose()
    {
        Console.Write("\x1B[u"); // Restore cursor position
    }

    public void Report(int value, int maximum)
    {
        lock (_lock)
        {
            var now = DateTime.Now;

            if (_lastOutput != default)
            {
                var elapsedSeconds = (now - _lastOutput).TotalSeconds;
                if (elapsedSeconds < 1)
                    return;
            }

            Console.Write("\x1B[u"); // Restore cursor position
            var percentage = Math.Round((float)value / maximum, 2, MidpointRounding.AwayFromZero);
            Console.WriteLine($"{percentage:P0}");
            _lastOutput = now;
        }
    }

    public static void Run(Action<IProgressMonitor> action)
    {
        using var reporter = new ConsoleReporter();
        action(reporter);
    }

    public static T Run<T>(Func<IProgressMonitor, T> action)
    {
        T result = default!;

        Run(pm =>
        {
            result = action(pm);
        });

        return result;
    }
}