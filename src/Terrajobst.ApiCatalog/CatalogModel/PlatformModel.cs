namespace Terrajobst.ApiCatalog;

public readonly struct PlatformModel : IEquatable<PlatformModel>
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _index;

    internal PlatformModel(ApiCatalogModel catalog, int index)
    {
        ThrowIfNull(catalog);
        ThrowIfRowIndexOutOfRange(catalog, ApiCatalogHeapOrTable.PlatformTable, index);

        _catalog = catalog;
        _index = index;
    }

    public int Id => _index;
    
    public ApiCatalogModel Catalog => _catalog;

    public string Name => ApiCatalogSchema.PlatformRow.Name.Read(_catalog, _index);

    public override bool Equals(object? obj)
    {
        return obj is PlatformModel model && Equals(model);
    }

    public bool Equals(PlatformModel other)
    {
        return ReferenceEquals(_catalog, other._catalog) &&
               _index == other._index;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_catalog, _index);
    }

    public static bool operator ==(PlatformModel left, PlatformModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PlatformModel left, PlatformModel right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return Name;
    }
}