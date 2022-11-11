using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel;

namespace Arroyo;

public struct MetadataCustomAttributeEnumerator : IEnumerable<MetadataCustomAttribute>, IEnumerator<MetadataCustomAttribute>
{
    public static MetadataCustomAttributeEnumerator Empty { get; } = new MetadataCustomAttributeEnumerator(ImmutableArray<MetadataCustomAttribute>.Empty, false);

    private readonly ImmutableArray<MetadataCustomAttribute> _attributes;
    private readonly bool _includeProcessed;
    private int _index;

    internal MetadataCustomAttributeEnumerator(ImmutableArray<MetadataCustomAttribute> attributes, bool includeProcessed)
    {
        _attributes = attributes;
        _includeProcessed = includeProcessed;
        _index = -1;
    }

    [EditorBrowsable(EditorBrowsableState.Never)] // Only here to make foreach work
    public MetadataCustomAttributeEnumerator GetEnumerator()
    {
        return this;
    }

    IEnumerator<MetadataCustomAttribute> IEnumerable<MetadataCustomAttribute>.GetEnumerator()
    {
        return this;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this;
    }

    public bool MoveNext()
    {
        while (true)
        {
            if (++_index >= _attributes.Length)
                return false;

            if (_includeProcessed || !_attributes[_index].IsProcessed)
                return true;
        }
    }

    void IEnumerator.Reset()
    {
        throw new NotImplementedException();
    }

    public MetadataCustomAttribute Current
    {
        get { return _attributes[_index]; }
    }

    object IEnumerator.Current
    {
        get { return Current; }
    }

    void IDisposable.Dispose()
    {
    }
}