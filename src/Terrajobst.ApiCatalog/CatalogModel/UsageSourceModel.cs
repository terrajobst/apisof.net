namespace Terrajobst.ApiCatalog;

public readonly struct UsageSourceModel : IEquatable<UsageSourceModel>
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _index;

    internal UsageSourceModel(ApiCatalogModel catalog, int index)
    {
        ThrowIfNull(catalog);
        ThrowIfRowIndexOutOfRange(catalog, ApiCatalogHeapOrTable.UsageSourceTable, index);

        _catalog = catalog;
        _index = index;
    }

    public int Id => _index;

    public ApiCatalogModel Catalog => _catalog;

    public string Name => ApiCatalogSchema.UsageSourceRow.Name.Read(_catalog, _index);

    public DateOnly Date => ApiCatalogSchema.UsageSourceRow.Date.Read(_catalog, _index);

    public override bool Equals(object? obj)
    {
        return obj is AssemblyModel model && Equals(model);
    }

    public bool Equals(UsageSourceModel other)
    {
        return ReferenceEquals(_catalog, other._catalog) &&
               _index == other._index;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_catalog, _index);
    }

    public static bool operator ==(UsageSourceModel left, UsageSourceModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UsageSourceModel left, UsageSourceModel right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"{Name} ({Date})";
    }
}