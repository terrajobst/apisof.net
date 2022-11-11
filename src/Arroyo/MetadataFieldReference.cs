namespace Arroyo;

public abstract class MetadataFieldReference : MetadataMember
{
    private protected MetadataFieldReference()
    {
    }

    public abstract MetadataType FieldType { get; }

    public abstract MetadataCustomAttributeEnumerator GetCustomAttributes(bool includeProcessed = false);
}