using System.Collections.Concurrent;

namespace Arroyo;

public static class GlobalStats
{
    private static int _countFile;
    private static int _countCustomAttribute;
    private static int _countIL;

    public static int Exceptions => _countFile + _countCustomAttribute + _countIL;

    public static int ExceptionsFile => _countFile;

    public static int ExceptionsCustomAttribute => _countCustomAttribute;

    public static int ExceptionsIL => _countIL;

    public static ConcurrentDictionary<string, int> ExceptionsAndCounts = new();

    internal static void IncrementFile()
    {
        Interlocked.Increment(ref _countFile);
    }

    public static void IncrementCustomAttributes()
    {
        Interlocked.Increment(ref _countCustomAttribute);
    }

    internal static void IncrementIL()
    {
        Interlocked.Increment(ref _countIL);
    }

    internal static void Report(Exception ex)
    {
        ExceptionsAndCounts.AddOrUpdate(ex.ToString(), 1, (k, v) => v + 1);
    }
}