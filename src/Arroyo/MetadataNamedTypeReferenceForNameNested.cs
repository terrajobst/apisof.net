namespace Arroyo;

internal sealed class MetadataNamedTypeReferenceForNameNested : MetadataNamedTypeReference
{
    private readonly MetadataNamedTypeReference _containingType;
    private readonly string _name;
    private readonly int _genericArity;

    internal MetadataNamedTypeReferenceForNameNested(MetadataNamedTypeReference containingType,
        string name)
    {
        _containingType = containingType;
        _name = SpecialNames.GetTypeName(name, out _genericArity);
        _genericArity = containingType.GenericArity;

        // NOTE: We'd don't need to adjust the generic arity because it's already in logical form.
    }

    public override int Token
    {
        get { return default; }
    }

    public override MetadataFile ContainingFile
    {
        get { return _containingType.ContainingFile; }
    }

    private protected override MetadataNamedTypeReference? ContainingTypeCore
    {
        get { return _containingType; }
    }

    public override string NamespaceName
    {
        get { return _containingType.NamespaceName; }
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