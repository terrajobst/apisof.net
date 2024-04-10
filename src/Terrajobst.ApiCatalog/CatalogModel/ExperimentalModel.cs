namespace Terrajobst.ApiCatalog;

public readonly struct ExperimentalModel
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _index;

    internal ExperimentalModel(ApiCatalogModel catalog, int index)
    {
        ThrowIfNull(catalog);
        ThrowIfRowIndexOutOfRange(catalog, ApiCatalogHeapOrTable.ExperimentalTable, index);

        _catalog = catalog;
        _index = index;
    }

    public int Id => _index;

    public ApiCatalogModel Catalog => _catalog;

    public string DiagnosticId => ApiCatalogSchema.ExperimentalRow.DiagnosticId.Read(_catalog, _index);

    public string UrlFormat => ApiCatalogSchema.ExperimentalRow.UrlFormat.Read(_catalog, _index);

    public string Url
    {
        get
        {
            return UrlFormat.Length > 0 && DiagnosticId.Length > 0
                        ? string.Format(UrlFormat, DiagnosticId)
                        : UrlFormat;
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is ExperimentalModel model && Equals(model);
    }

    public bool Equals(ExperimentalModel other)
    {
        return ReferenceEquals(_catalog, other._catalog) &&
               _index == other._index;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_catalog, _index);
    }

    public static bool operator ==(ExperimentalModel left, ExperimentalModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ExperimentalModel left, ExperimentalModel right)
    {
        return !(left == right);
    }
}