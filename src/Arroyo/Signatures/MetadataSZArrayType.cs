using System.Collections.Immutable;

namespace Arroyo.Signatures;

internal sealed class MetadataSZArrayType : MetadataArrayType
{
    private readonly MetadataType _elementType;

    internal MetadataSZArrayType(MetadataType elementType)
    {
        _elementType = elementType;
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
        get { return 0; }
    }

    public override ImmutableArray<int> LowerBounds
    {
        get { return ImmutableArray<int>.Empty; }
    }

    public override ImmutableArray<int> Sizes
    {
        get { return ImmutableArray<int>.Empty; }
    }
}