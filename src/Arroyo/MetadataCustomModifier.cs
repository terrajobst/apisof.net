namespace Arroyo;

public readonly struct MetadataCustomModifier
{
    private readonly MetadataNamedTypeReference _type;
    private readonly bool _isRequired;

    internal MetadataCustomModifier(MetadataNamedTypeReference type, bool isRequired)
    {
        _type = type;
        _isRequired = isRequired;
    }

    public MetadataNamedTypeReference Type
    {
        get { return _type; }
    }

    public bool IsRequired
    {
        get { return _isRequired; }
    }
}