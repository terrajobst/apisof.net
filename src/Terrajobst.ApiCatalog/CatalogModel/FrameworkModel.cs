using System.Collections;
using NuGet.Frameworks;

namespace Terrajobst.ApiCatalog;

public readonly struct FrameworkModel : IEquatable<FrameworkModel>
{
    private readonly ApiCatalogModel _catalog;
    private readonly int _index;

    internal FrameworkModel(ApiCatalogModel catalog, int index)
    {
        ThrowIfNull(catalog);
        ThrowIfRowIndexOutOfRange(catalog, ApiCatalogHeapOrTable.FrameworkTable, index);

        _catalog = catalog;
        _index = index;
    }

    public int Id => _index;

    public ApiCatalogModel Catalog => _catalog;

    public string Name => ApiCatalogSchema.FrameworkRow.Name.Read(_catalog, _index);

    public bool IsPreview => _catalog.IsPreviewFramework(this);

    public NuGetFramework NuGetFramework => _catalog.AvailabilityContext.GetNuGetFramework(_index);

    public AssemblyEnumerator Assemblies
    {
        get
        {
            var enumerator = ApiCatalogSchema.FrameworkRow.Assemblies.Read(_catalog, _index);
            return new AssemblyEnumerator(enumerator);
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is FrameworkModel model && Equals(model);
    }

    public bool Equals(FrameworkModel other)
    {
        return ReferenceEquals(_catalog, other._catalog) &&
               _index == other._index;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_catalog, _index);
    }

    public static bool operator ==(FrameworkModel left, FrameworkModel right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FrameworkModel left, FrameworkModel right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return Name;
    }

    public struct AssemblyEnumerator : IEnumerable<AssemblyModel>, IEnumerator<AssemblyModel>
    {
        private ApiCatalogSchema.ArrayEnumerator<AssemblyModel> _enumerator;

        internal AssemblyEnumerator(ApiCatalogSchema.ArrayEnumerator<AssemblyModel> enumerator)
        {
            _enumerator = enumerator;
        }

        IEnumerator<AssemblyModel> IEnumerable<AssemblyModel>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public AssemblyEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public AssemblyModel Current
        {
            get { return _enumerator.Current; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
        }
    }
}