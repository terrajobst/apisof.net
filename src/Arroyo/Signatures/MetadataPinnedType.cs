namespace Arroyo.Signatures;

public sealed class MetadataPinnedType : MetadataType
{
    private readonly MetadataType _elementType;

    internal MetadataPinnedType(MetadataType elementType)
    {
        _elementType = elementType;
    }

    public override int Token
    {
        get { return default; }
    }

    public MetadataType ElementType
    {
        get { return _elementType; }
    }
}