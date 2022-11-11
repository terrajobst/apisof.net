using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Arroyo;

internal sealed class MetadataNamedTypeReferenceForHandle : MetadataNamedTypeReference
{
    private readonly MetadataFile _containingFile;
    private readonly TypeReferenceHandle _handle;
    private readonly object _resolutionScope;
    private readonly string _name;
    private readonly string _namespaceName;
    private readonly int _genericArity;

    internal MetadataNamedTypeReferenceForHandle(MetadataModule containingModule, TypeReferenceHandle handle)
    {
        _handle = handle;

        var reference = containingModule.MetadataReader.GetTypeReference(handle);

        switch (reference.ResolutionScope.Kind)
        {
            case HandleKind.ModuleDefinition:
                _resolutionScope = containingModule;
                _containingFile = containingModule;
                break;
            case HandleKind.AssemblyReference:
                var assemblyReference = containingModule.GetAssemblyReference((AssemblyReferenceHandle)reference.ResolutionScope);
                _resolutionScope = assemblyReference;
                _containingFile = assemblyReference;
                break;
            case HandleKind.TypeReference:
                var typeReference = containingModule.GetNamedTypeReference((TypeReferenceHandle)reference.ResolutionScope);
                _resolutionScope = typeReference;
                _containingFile = typeReference.ContainingFile;
                break;
            default:
                throw new Exception($"Unexpected resolution scope: {reference.ResolutionScope.Kind}");
        }

        _name = SpecialNames.GetTypeName(containingModule.MetadataReader.GetString(reference.Name), out _genericArity);
        _namespaceName = containingModule.MetadataReader.GetString(reference.Namespace);

        var c = ContainingType;
        while (c is not null)
        {
            _genericArity -= c.GenericArity;
            c = c.ContainingType;
        }
    }

    public override int Token
    {
        get { return MetadataTokens.GetToken(_handle); }
    }

    public object ResolutionScope
    {
        get { return _resolutionScope; }
    }

    public override MetadataFile ContainingFile
    {
        get { return _containingFile; }
    }

    private protected override MetadataNamedTypeReference? ContainingTypeCore
    {
        get { return ResolutionScope as MetadataNamedTypeReference; }
    }

    public override string Name
    {
        get { return _name; }
    }

    public override string NamespaceName
    {
        get { return _namespaceName; }
    }

    public override int GenericArity
    {
        get { return _genericArity; }
    }
}