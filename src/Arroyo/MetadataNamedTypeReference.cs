namespace Arroyo;

public abstract class MetadataNamedTypeReference : MetadataType
{
    private protected MetadataNamedTypeReference()
    {
    }

    public abstract MetadataFile ContainingFile { get; }

    public MetadataNamedTypeReference? ContainingType
    {
        get { return ContainingTypeCore; }
    }

    public abstract string Name { get; }

    public abstract string NamespaceName { get; }

    public abstract int GenericArity { get; }

    private protected abstract MetadataNamedTypeReference? ContainingTypeCore { get; }

    public string GetFullName()
    {
        if (ContainingType is not null)
            return $"{ContainingType.GetFullName()}.{Name}";

        return string.IsNullOrEmpty(NamespaceName)
            ? Name
            : NamespaceName + "." + Name;
    }
}