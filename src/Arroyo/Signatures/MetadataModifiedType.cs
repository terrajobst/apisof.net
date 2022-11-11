namespace Arroyo.Signatures;

public sealed class MetadataModifiedType : MetadataType
{
    private readonly MetadataType _unmodifiedType;
    private readonly MetadataCustomModifier _customModifier;

    internal MetadataModifiedType(MetadataType unmodifiedType, MetadataCustomModifier customModifier)
    {
        _unmodifiedType = unmodifiedType;
        _customModifier = customModifier;
    }

    public override int Token
    {
        get { return default; }
    }

    public MetadataType UnmodifiedType
    {
        get { return _unmodifiedType; }
    }

    public MetadataCustomModifier CustomModifier
    {
        get { return _customModifier; }
    }
}