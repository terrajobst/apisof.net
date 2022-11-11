namespace Arroyo;

internal sealed class MetadataNamedTypeReferenceForName : MetadataNamedTypeReference
{
    private readonly MetadataFile _containingFile;
    private readonly string _namespaceName;
    private readonly string _name;
    private readonly int _genericArity;

    internal MetadataNamedTypeReferenceForName(MetadataFile containingFile,
                                               string namespaceName,
                                               string name)
    {
        _containingFile = containingFile;
        _namespaceName = namespaceName;
        _name = SpecialNames.GetTypeName(name, out _genericArity);
    }

    public override int Token
    {
        get { return default; }
    }

    public override MetadataFile ContainingFile
    {
        get { return _containingFile; }
    }

    private protected override MetadataNamedTypeReference? ContainingTypeCore
    {
        get { return null; }
    }

    public override string NamespaceName
    {
        get { return _namespaceName; }
    }

    public override string Name
    {
        get { return _name; }
    }

    public override int GenericArity
    {
        get { return _genericArity; }
    }
}