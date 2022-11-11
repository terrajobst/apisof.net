namespace Arroyo.Signatures;

public sealed class MetadataByReferenceType : MetadataType
{
    private readonly MetadataType _elementType;

    internal MetadataByReferenceType(MetadataType elementType)
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