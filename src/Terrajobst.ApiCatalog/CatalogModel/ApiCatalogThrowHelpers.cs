global using static Terrajobst.ApiCatalog.ApiCatalogThrowHelpers;

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Terrajobst.ApiCatalog;

internal static class ApiCatalogThrowHelpers
{
    public static void ThrowIfRowIndexOutOfRange(ApiCatalogModel catalog, ApiCatalogHeapOrTable table, int rowIndex)
    {
        catalog.GetMemory(table, out var memory, out var rowSize);
        Debug.Assert(memory.Length % rowSize == 0);
        var rowCount = memory.Length / rowSize;
        
        ThrowIfNegative(rowIndex);
        ThrowIfGreaterThanOrEqual(rowIndex, rowCount);
    }

    internal static void ThrowIfBlobOffsetOutOfRange(ApiCatalogModel catalog, int offset)
    {
        ThrowIfNegative(offset);
        ThrowIfGreaterThanOrEqual(offset, catalog.BlobHeap.Length);
    }

    public static void ThrowIfDefault<T>(T value, [CallerArgumentExpression("value")] string? paramName = null)
        where T: struct, IEquatable<T>
    {
        if (value.Equals(default))
            throw new ArgumentException($"The value representing {typeof(T).Name} must be initialized.", paramName);
    }
}