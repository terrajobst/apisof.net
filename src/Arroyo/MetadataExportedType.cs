using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Arroyo;

public sealed class MetadataExportedType
{
    private readonly MetadataModule _containingModule;
    private readonly ExportedTypeHandle _handle;
    private readonly TypeAttributes _attributes;
    private readonly bool _isForwarder;
    private readonly MetadataNamedTypeReference _reference;

    internal MetadataExportedType(MetadataModule containingModule, ExportedTypeHandle handle)
    {
        _containingModule = containingModule;
        _handle = handle;

        var definition = containingModule.MetadataReader.GetExportedType(handle);
        _attributes = definition.Attributes;
        _isForwarder = definition.IsForwarder;

        var namespaceName = containingModule.MetadataReader.GetString(definition.Namespace);
        var name = containingModule.MetadataReader.GetString(definition.Name);

        switch (definition.Implementation.Kind)
        {
            case HandleKind.AssemblyReference:
                var assemblyReference = containingModule.GetAssemblyReference((AssemblyReferenceHandle)definition.Implementation);
                _reference = new MetadataNamedTypeReferenceForName(assemblyReference, namespaceName, name);
                break;
            case HandleKind.TypeReference:
                var typeReference = containingModule.GetNamedTypeReference((TypeReferenceHandle)definition.Implementation);
                _reference = new MetadataNamedTypeReferenceForNameNested(typeReference, name);
                break;
            default:
                throw new Exception($"Unexpected implementation: {definition.Implementation.Kind}");
        }
    }

    public int Token
    {
        get { return MetadataTokens.GetToken(_handle); }
    }

    public MetadataModule ContainingModule
    {
        get { return _containingModule; }
    }

    public TypeAttributes Attributes
    {
        get { return _attributes; }
    }

    public bool IsForwarder
    {
        get { return _isForwarder; }
    }

    public MetadataNamedTypeReference Reference
    {
        get { return _reference; }
    }
}