namespace Arroyo.Signatures;

public sealed class MetadataPointerType : MetadataType
{
    private readonly MetadataType _elementType;

    internal MetadataPointerType(MetadataType elementType)
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