namespace Terrajobst.ApiCatalog;

public readonly struct ObsoletionModel : IEquatable<ObsoletionModel>
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _index;

    internal ObsoletionModel(ApiCatalogModel catalog, int index)
    {
        ThrowIfNull(catalog);
        ThrowIfRowIndexOutOfRange(catalog, ApiCatalogHeapOrTable.ObsoletionTable, index);

        _catalog = catalog;
        _index = index;
    }

    public int Id => _index;

    public ApiCatalogModel Catalog => _catalog;

    public string Message => ApiCatalogSchema.ObsoletionRow.Message.Read(_catalog, _index);

    public bool IsError => ApiCatalogSchema.ObsoletionRow.IsError.Read(_catalog, _index);

    public string DiagnosticId => ApiCatalogSchema.ObsoletionRow.DiagnosticId.Read(_catalog, _index);

    public string UrlFormat => ApiCatalogSchema.ObsoletionRow.UrlFormat.Read(_catalog, _index);

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
        return obj is ObsoletionModel model && Equals(model);
    }

    public bool Equals(ObsoletionModel other)
    {
        return ReferenceEquals(_catalog, other._catalog) &&
               _index == other._index;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_catalog, _index);
    }

    public static bool operator ==(ObsoletionModel left, ObsoletionModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ObsoletionModel left, ObsoletionModel right)
    {
        return !(left == right);
    }
}