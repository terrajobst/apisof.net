namespace Arroyo;

public abstract class MetadataAssemblyReference : MetadataFile
{
    private protected MetadataAssemblyReference()
    {
    }

    public abstract MetadataAssemblyIdentity Identity { get; }

    public override string ToString()
    {
        return Identity.ToString();
    }
}