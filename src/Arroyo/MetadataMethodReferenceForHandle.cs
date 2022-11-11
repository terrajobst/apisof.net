using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Arroyo;

internal sealed class MetadataMethodReferenceForHandle : MetadataMethodReference
{
    private readonly MemberReferenceHandle _handle;
    private readonly MetadataType _containingType;
    private readonly string _name;
    private readonly int _genericArity;
    private readonly MetadataSignature _signature;
    private readonly ImmutableArray<MetadataCustomAttribute> _customAttributes;

    internal MetadataMethodReferenceForHandle(MetadataModule containingModule, MemberReferenceHandle handle, MemberReference reference)
    {
        Debug.Assert(reference.GetKind() == MemberReferenceKind.Method);

        switch (reference.Parent.Kind)
        {
            case HandleKind.TypeReference:
            case HandleKind.TypeDefinition:
            case HandleKind.TypeSpecification:
                _containingType = containingModule.GetTypeReference(reference.Parent, default);
                break;
            case HandleKind.MethodDefinition:
                // TODO: What does it mean if a method is contained in a another method?
                // It seems C++/CLI emits that for some stuff on the module type.
                var methodHandle = (MethodDefinitionHandle)reference.Parent;
                var methodDef = containingModule.MetadataReader.GetMethodDefinition(methodHandle);
                var methodDefType = containingModule.GetNamedType(methodDef.GetDeclaringType());
                var method = methodDefType.GetMethod(methodHandle);
                _containingType = method.ContainingType;
                break;
            default:
                throw new Exception($"Unexpected parent kind {reference.Parent.Kind}");
        }

        _handle = handle;
        _name = SpecialNames.GetMethodName(containingModule.MetadataReader.GetString(reference.Name), out _genericArity);
        _signature = containingModule.GetMethodSignature(reference.Signature, default);
        _customAttributes = containingModule.GetCustomAttributes(reference.GetCustomAttributes());
    }

    public override int Token
    {
        get { return MetadataTokens.GetToken(_handle); }
    }

    private protected override MetadataType ContainingTypeCore
    {
        get { return _containingType; }
    }

    public override string Name
    {
        get { return _name; }
    }

    public override int GenericArity
    {
        get { return _genericArity; }
    }

    public override MetadataSignature Signature
    {
        get { return _signature; }
    }

    public override MetadataCustomAttributeEnumerator GetCustomAttributes(bool includeProcessed = false)
    {
        return new MetadataCustomAttributeEnumerator(_customAttributes, includeProcessed);
    }
}