namespace Arroyo;

public abstract class MetadataMember : MetadataItem
{
    private protected MetadataMember()
    {
    }

    public MetadataType ContainingType
    {
        get { return ContainingTypeCore; }
    }

    public abstract string Name { get; }

    private protected abstract MetadataType ContainingTypeCore { get; }
}