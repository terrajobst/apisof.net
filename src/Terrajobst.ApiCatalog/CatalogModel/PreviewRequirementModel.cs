namespace Terrajobst.ApiCatalog;

public readonly struct PreviewRequirementModel
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _index;

    internal PreviewRequirementModel(ApiCatalogModel catalog, int index)
    {
        ThrowIfNull(catalog);
        ThrowIfRowIndexOutOfRange(catalog, ApiCatalogHeapOrTable.PreviewRequirementTable, index);

        _catalog = catalog;
        _index = index;
    }

    public int Id => _index;

    public ApiCatalogModel Catalog => _catalog;

    public string Message => ApiCatalogSchema.PreviewRequirementRow.Message.Read(_catalog, _index);

    public string Url => ApiCatalogSchema.PreviewRequirementRow.Url.Read(_catalog, _index);

    public override bool Equals(object? obj)
    {
        return obj is PreviewRequirementModel model && Equals(model);
    }

    public bool Equals(PreviewRequirementModel other)
    {
        return ReferenceEquals(_catalog, other._catalog) &&
               _index == other._index;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_catalog, _index);
    }

    public static bool operator ==(PreviewRequirementModel left, PreviewRequirementModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PreviewRequirementModel left, PreviewRequirementModel right)
    {
        return !(left == right);
    }
}