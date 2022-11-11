using System.Collections.Immutable;

namespace Arroyo.Signatures;

internal sealed class MetadataArrayTypeImpl : MetadataArrayType
{
    private readonly MetadataType _elementType;
    private readonly int _rank;
    private readonly ImmutableArray<int> _lowerBounds;
    private readonly ImmutableArray<int> _sizes;

    internal MetadataArrayTypeImpl(MetadataType elementType, int rank, ImmutableArray<int> lowerBounds, ImmutableArray<int> sizes)
    {
        _elementType = elementType;
        _rank = rank;
        _lowerBounds = lowerBounds;
        _sizes = sizes;
    }

    public override int Token
    {
        get { return default; }
    }

    public override MetadataType ElementType
    {
        get { return _elementType; }
    }

    public override int Rank
    {
        get { return _rank; }
    }

    public override ImmutableArray<int> LowerBounds
    {
        get { return _lowerBounds; }
    }

    public override ImmutableArray<int> Sizes
    {
        get { return _sizes; }
    }
}