namespace Terrajobst.ApiCatalog;

public readonly struct ExtensionMethodModel : IEquatable<ExtensionMethodModel>
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _index;

    internal ExtensionMethodModel(ApiCatalogModel catalog, int index)
    {
        ThrowIfNull(catalog);
        ThrowIfRowIndexOutOfRange(catalog, ApiCatalogHeapOrTable.ExtensionMethodTable, index);

        _catalog = catalog;
        _index = index;
    }

    public int Id => _index;

    public ApiCatalogModel Catalog => _catalog;

    public Guid Guid => ApiCatalogSchema.ExtensionMethodRow.Guid.Read(_catalog, _index);

    public ApiModel ExtendedType => ApiCatalogSchema.ExtensionMethodRow.ExtendedType.Read(_catalog, _index);

    public ApiModel ExtensionMethod => ApiCatalogSchema.ExtensionMethodRow.ExtensionMethod.Read(_catalog, _index);

    public override bool Equals(object? obj)
    {
        return obj is ExtensionMethodModel model && Equals(model);
    }

    public bool Equals(ExtensionMethodModel other)
    {
        return ReferenceEquals(_catalog, other._catalog) &&
               _index == other._index;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_catalog, _index);
    }

    public static bool operator ==(ExtensionMethodModel left, ExtensionMethodModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ExtensionMethodModel left, ExtensionMethodModel right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return ExtensionMethod.ToString();
    }
}