namespace Arroyo;

public abstract class MetadataMethodReference : MetadataMember
{
    private protected MetadataMethodReference()
    {
    }

    public abstract int GenericArity { get; }

    public abstract MetadataSignature Signature { get; }

    public abstract MetadataCustomAttributeEnumerator GetCustomAttributes(bool includeProcessed = false);
}