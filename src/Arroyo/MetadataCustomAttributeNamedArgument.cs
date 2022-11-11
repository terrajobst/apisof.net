using System.Reflection.Metadata;

namespace Arroyo;

public readonly struct MetadataCustomAttributeNamedArgument
{
    private readonly string _name;
    private readonly CustomAttributeNamedArgumentKind _kind;
    private readonly MetadataTypedValue _value;

    public MetadataCustomAttributeNamedArgument(string name,
                                                CustomAttributeNamedArgumentKind kind,
                                                MetadataTypedValue value)
    {
        _name = name;
        _kind = kind;
        _value = value;
    }

    public string Name
    {
        get { return _name; }
    }

    public CustomAttributeNamedArgumentKind Kind
    {
        get { return _kind; }
    }

    public MetadataTypedValue Value
    {
        get { return _value; }
    }
}