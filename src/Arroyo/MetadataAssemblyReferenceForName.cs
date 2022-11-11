namespace Arroyo;

internal sealed class MetadataAssemblyReferenceForName : MetadataAssemblyReference
{
    private readonly MetadataAssemblyIdentity _identity;

    internal MetadataAssemblyReferenceForName(string qualifiedName)
    {
        _identity = MetadataAssemblyIdentity.Parse(qualifiedName);
    }

    public override int Token
    {
        get { return default; }
    }

    public override string Name
    {
        get { return _identity.Name; }
    }

    public override MetadataAssemblyIdentity Identity
    {
        get { return _identity; }
    }

    public override MetadataCustomAttributeEnumerator GetCustomAttributes(bool includeProcessed = false)
    {
        return MetadataCustomAttributeEnumerator.Empty;
    }
}