using System.Collections.Immutable;

namespace Arroyo.Signatures;

public abstract class MetadataArrayType : MetadataType
{
    private protected MetadataArrayType()
    {
    }

    public abstract MetadataType ElementType { get; }

    public abstract int Rank { get; }

    public abstract ImmutableArray<int> LowerBounds { get; }

    public abstract ImmutableArray<int> Sizes { get; }
}