using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Arroyo;

public sealed class MetadataMethodInstance : MetadataMethodReference
{
    private readonly MethodSpecificationHandle _handle;
    private readonly MetadataMethodReference _method;
    private readonly ImmutableArray<MetadataType> _typeArguments;
    private readonly ImmutableArray<MetadataCustomAttribute> _customAttributes;

    public MetadataMethodInstance(MetadataModule containingModule, MethodSpecificationHandle handle)
    {
        var specification = containingModule.MetadataReader.GetMethodSpecification(handle);
        _handle = handle;
        _typeArguments = specification.DecodeSignature(containingModule.TypeProvider, default);
        _method = containingModule.GetMethodReference(specification.Method);
        _customAttributes = containingModule.GetCustomAttributes(specification.GetCustomAttributes());
    }

    public override int Token
    {
        get { return MetadataTokens.GetToken(_handle); }
    }

    private protected override MetadataType ContainingTypeCore
    {
        get { return Method.ContainingType; }
    }

    public MetadataMethodReference Method
    {
        get { return _method; }
    }

    public override string Name
    {
        get { return Method.Name; }
    }

    public override int GenericArity
    {
        // TODO: Is this actually true?
        get { return 0; }
    }

    public override MetadataSignature Signature
    {
        get { return Method.Signature; }
    }

    public ImmutableArray<MetadataType> TypeArguments
    {
        get { return _typeArguments; }
    }

    public override MetadataCustomAttributeEnumerator GetCustomAttributes(bool includeProcessed = false)
    {
        return new MetadataCustomAttributeEnumerator(_customAttributes, includeProcessed);
    }
}