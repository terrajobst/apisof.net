using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Arroyo.Signatures;

namespace Arroyo;

public sealed class MetadataModule : MetadataFile, IDisposable
{
    private readonly MetadataAssembly? _containingAssembly;
    private readonly PEReader _peReader;
    private readonly MetadataReader _metadataReader;
    private readonly string _location;
    private readonly MetadataTypeProvider _typeProvider;
    private readonly int _generation;
    private readonly Guid _baseGenerationId;
    private readonly Guid _generationId;
    private readonly Guid _mvid;

    private MetadataNamedTypeReference[]? _specialTypes;
    private string? _name;
    private MetadataNamespace? _namespaceRoot;
    private ImmutableArray<MetadataNamedType> _types;
    private ImmutableArray<MetadataExportedType> _exportedTypes;
    private ImmutableArray<MetadataCustomAttribute> _customAttributes;
    private ImmutableArray<MetadataAssemblyReference> _assemblyReferences;
    private ImmutableArray<MetadataFileReference> _moduleReferences;
    private Dictionary<int, MetadataNamedType>? _typeByHandle;

    private readonly ConcurrentDictionary<string, int> _committedEnumSizes = new ConcurrentDictionary<string, int>();

    internal MetadataModule(MetadataAssembly? containingAssembly,
                            PEReader peReader,
                            MetadataReader metadataReader,
                            string location)
    {
        _containingAssembly = containingAssembly;
        _peReader = peReader;
        _metadataReader = metadataReader;
        _location = location;
        var definition = _metadataReader.GetModuleDefinition();
        _typeProvider = new MetadataTypeProvider(this);
        _generation = definition.Generation;
        _generationId = _metadataReader.GetGuid(definition.GenerationId);
        _baseGenerationId = _metadataReader.GetGuid(definition.BaseGenerationId);
        _mvid = _metadataReader.GetGuid(definition.Mvid);
    }

    public void Dispose()
    {
        _peReader.Dispose();
    }

    internal ConcurrentDictionary<string, int> CommittedEnumSizes
    {
        get { return _committedEnumSizes; }
    }

    public static new MetadataModule? Open(string path)
    {
        return MetadataFile.Open(path) as MetadataModule;
    }

    public static new MetadataModule? Open(Stream stream)
    {
        return MetadataFile.Open(stream) as MetadataModule;
    }

    public static new MetadataModule? Open(Stream stream, string location)
    {
        return MetadataFile.Open(stream, location) as MetadataModule;
    }

    internal PEReader PEReader
    {
        get { return _peReader; }
    }

    internal MetadataReader MetadataReader
    {
        get { return _metadataReader; }
    }

    internal MetadataTypeProvider TypeProvider
    {
        get { return _typeProvider; }
    }

    public override int Token
    {
        get { return default; }
    }

    public MetadataAssembly? ContainingAssembly
    {
        get { return _containingAssembly; }
    }

    public override string Name
    {
        get
        {
            if (_name is null)
            {
                var name = ComputeName();
                Interlocked.CompareExchange(ref _name, name, null);
            }

            return _name;
        }
    }

    public string Location
    {
        get { return _location; }
    }

    public Guid BaseGenerationId
    {
        get { return _baseGenerationId; }
    }

    public int Generation
    {
        get { return _generation; }
    }

    public Guid GenerationId
    {
        get { return _generationId; }
    }

    public Guid Mvid
    {
        get { return _mvid; }
    }

    public MetadataNamespace NamespaceRoot
    {
        get
        {
            if (_namespaceRoot is null)
            {
                var namespaceDefinition = ComputeNamespaceRoot();
                Interlocked.CompareExchange(ref _namespaceRoot, namespaceDefinition, null);
            }

            return _namespaceRoot;
        }
    }

    public ImmutableArray<MetadataNamedType> Types
    {
        get
        {
            if (_types.IsDefault)
            {
                var types = ComputeTypes();
                ImmutableInterlocked.InterlockedInitialize(ref _types, types);
            }

            return _types;
        }
    }

    public ImmutableArray<MetadataExportedType> ExportedTypes
    {
        get
        {
            if (_exportedTypes.IsDefault)
            {
                var exportedTypes = ComputeExportedTypes();
                ImmutableInterlocked.InterlockedInitialize(ref _exportedTypes, exportedTypes);
            }

            return _exportedTypes;
        }
    }

    public override MetadataCustomAttributeEnumerator GetCustomAttributes(bool includeProcessed = false)
    {
        if (_customAttributes.IsDefault)
        {
            var customAttributes = ComputeCustomAttributes();
            ImmutableInterlocked.InterlockedInitialize(ref _customAttributes, customAttributes);
        }

        return new MetadataCustomAttributeEnumerator(_customAttributes, includeProcessed);
    }

    public ImmutableArray<MetadataAssemblyReference> AssemblyReferences
    {
        get
        {
            if (_assemblyReferences.IsDefault)
            {
                var assemblyReferences = ComputeAssemblyReferences();
                ImmutableInterlocked.InterlockedInitialize(ref _assemblyReferences, assemblyReferences);
            }

            return _assemblyReferences;
        }
    }

    public ImmutableArray<MetadataFileReference> FileReferences
    {
        get
        {
            if (_moduleReferences.IsDefault)
            {
                var moduleReferences = ComputeFileReferences();
                ImmutableInterlocked.InterlockedInitialize(ref _moduleReferences, moduleReferences);
            }
            return _moduleReferences;
        }
    }

    public IEnumerable<MetadataNamedTypeReference> GetTypeReferences()
    {
        foreach (var typeReference in _metadataReader.TypeReferences)
            yield return GetNamedTypeReference(typeReference);
    }

    public MetadataNamedTypeReference GetSpecialType(MetadataSpecialType specialType)
    {
        if (_specialTypes is null)
        {
            var specialTypes = ComputeSpecialTypes();
            Interlocked.CompareExchange(ref _specialTypes, specialTypes, null);
        }

        return _specialTypes[(int)specialType];
    }

    public IEnumerable<MetadataMember> GetMemberReferences()
    {
        foreach (var memberReference in _metadataReader.MemberReferences)
            yield return GetMemberReference(memberReference);
    }

    internal MetadataAssemblyReference GetAssemblyReference(AssemblyReferenceHandle assemblyReferenceHandle)
    {
        // TODO: Should these be interned?
        return new MetadataAssemblyReferenceForHandle(this, assemblyReferenceHandle);
    }

    internal MetadataNamedType GetNamedType(TypeDefinitionHandle handle)
    {
        if (_typeByHandle is null)
        {
            var typeByToken = Types.ToDictionary(t => t.Token);
            Interlocked.CompareExchange(ref _typeByHandle, typeByToken, null);
        }

        var token = MetadataTokens.GetToken(handle);
        return _typeByHandle[token];
    }

    private MetadataType GetSpecifiedType(TypeSpecificationHandle handle, MetadataGenericContext genericContext)
    {
        // TODO: Should these be interned?
        var specification = _metadataReader.GetTypeSpecification(handle);
        return specification.DecodeSignature(_typeProvider, genericContext);
    }

    internal MetadataNamedTypeReference GetNamedTypeReference(TypeReferenceHandle handle)
    {
        // TODO: Should these be interned?
        return new MetadataNamedTypeReferenceForHandle(this, handle);
    }

    internal MetadataType GetTypeReference(Handle handle, MetadataGenericContext genericContext)
    {
        switch (handle.Kind)
        {
            case HandleKind.TypeDefinition:
                return GetNamedType((TypeDefinitionHandle)handle);
            case HandleKind.TypeSpecification:
                return GetSpecifiedType((TypeSpecificationHandle)handle, genericContext);
            case HandleKind.TypeReference:
                return GetNamedTypeReference((TypeReferenceHandle)handle);
            default:
                throw new Exception($"Unexpected handle kind {handle.Kind}");
        }
    }

    internal MetadataMember GetMemberReference(Handle handle)
    {
        switch (handle.Kind)
        {
            case HandleKind.FieldDefinition:
                var fieldHandle = (FieldDefinitionHandle)handle;
                var fieldDefinition = MetadataReader.GetFieldDefinition(fieldHandle);
                var fieldType = GetNamedType(fieldDefinition.GetDeclaringType());
                return fieldType.GetField(fieldHandle);
            case HandleKind.MethodDefinition:
                var methodHandle = (MethodDefinitionHandle)handle;
                var methodDefinition = MetadataReader.GetMethodDefinition(methodHandle);
                var methodType = GetNamedType(methodDefinition.GetDeclaringType());
                return methodType.GetMethod(methodHandle);
            case HandleKind.MethodSpecification:
                return GetMethodInstance((MethodSpecificationHandle)handle);
            case HandleKind.MemberReference:
                return GetMemberReference((MemberReferenceHandle)handle);
            default:
                throw new Exception($"Unexpected handle kind: {handle.Kind}");
        }
    }

    internal MetadataMethodReference GetMethodReference(Handle handle)
    {
        switch (handle.Kind)
        {
            case HandleKind.MethodDefinition:
            {
                var methodHandle = (MethodDefinitionHandle)handle;
                var methodDefinition = MetadataReader.GetMethodDefinition(methodHandle);
                var type = GetNamedType(methodDefinition.GetDeclaringType());
                return type.GetMethod(methodHandle);
            }
            case HandleKind.MemberReference:
            {
                var token = (MemberReferenceHandle)handle;
                return (MetadataMethodReference)GetMemberReference(token);
            }
            default:
                throw new Exception($"Unexpected handle kind: {handle.Kind}");
        }
    }

    private MetadataMethodInstance GetMethodInstance(MethodSpecificationHandle handle)
    {
        // TODO: Should these be interned?
        return new MetadataMethodInstance(this, handle);
    }

    private MetadataMember GetMemberReference(MemberReferenceHandle handle)
    {
        var reference = MetadataReader.GetMemberReference(handle);
        var kind = reference.GetKind();
        switch (kind)
        {
            case MemberReferenceKind.Method:
                return GetMethodReference(handle, reference);
            case MemberReferenceKind.Field:
                return GetFieldReference(handle, reference);
            default:
                throw new Exception($"Unexpected handle kind: {kind}");
        }
    }

    private MetadataMember GetMethodReference(MemberReferenceHandle handle, MemberReference reference)
    {
        // TODO: Should these be interned?
        return new MetadataMethodReferenceForHandle(this, handle, reference);
    }

    private MetadataFieldReference GetFieldReference(MemberReferenceHandle handle, MemberReference reference)
    {
        // TODO: Should these be interned?
        return new MetadataFieldReferenceForHandle(this, handle, reference);
    }

    internal MetadataSignature GetMethodSignature(BlobHandle blobHandle, MetadataGenericContext genericContext)
    {
        var decoder = new SignatureDecoder<MetadataType, MetadataGenericContext>(_typeProvider, _metadataReader, genericContext);
        var blobReader = _metadataReader.GetBlobReader(blobHandle);
        var signature = decoder.DecodeMethodSignature(ref blobReader);
        return new MetadataSignature(signature);
    }

    internal ImmutableArray<MetadataCustomAttribute> GetCustomAttributes(CustomAttributeHandleCollection attributes)
    {
        var result = ImmutableArray.CreateBuilder<MetadataCustomAttribute>(attributes.Count);

        foreach (var customAttributeHandle in attributes)
        {
            var ca = new MetadataCustomAttribute(this, customAttributeHandle);
            if (ca.Constructor.ContainingType is MetadataNamedTypeReference attributedType)
            {
                if (attributedType.Name == "CompilerFeatureRequiredAttribute" &&
                    attributedType.NamespaceName == "System.Runtime.CompilerServices" &&
                    ca.FixedArguments.Length == 1 &&
                    ca.NamedArguments.Length == 0 &&
                    ca.FixedArguments[0].Value is string featureName &&
                    featureName is "RefStructs" or "RequiredMembers")
                {
                    ca.MarkProcessed();
                }

                if (attributedType.Name == "ObsoleteAttribute" &&
                    attributedType.NamespaceName == "System" &&
                    ca.FixedArguments.Length == 2 &&
                    ca.NamedArguments.Length == 0 &&
                    ca.FixedArguments[0].Value is string obsoletedMessage &&
                    ca.FixedArguments[1].Value is bool isError &&
                    obsoletedMessage is "Constructors of types with required members are not supported in this version of your compiler." or
                                        "Types with embedded references are not supported in this version of your compiler." &&
                    isError)
                {
                    ca.MarkProcessed();
                }
            }

            result.Add(ca);
        }

        return result.ToImmutable();
    }

    private string ComputeName()
    {
        var definition = _metadataReader.GetModuleDefinition();
        return _metadataReader.GetString(definition.Name);
    }

    private MetadataNamespace ComputeNamespaceRoot()
    {
        var namespaceDefinitionRoot = _metadataReader.GetNamespaceDefinitionRoot();
        return new MetadataNamespace(this, null, default, namespaceDefinitionRoot);
    }

    private ImmutableArray<MetadataNamedType> ComputeTypes()
    {
        var result = ImmutableArray.CreateBuilder<MetadataNamedType>(_metadataReader.TypeDefinitions.Count);

        AddTypes(result, NamespaceRoot);

        Debug.Assert(result.Count == _metadataReader.TypeDefinitions.Count);

        return result.ToImmutable();
    }

    private static void AddTypes(ICollection<MetadataNamedType> receiver, MetadataNamespace @namespace)
    {
        foreach (var member in @namespace.Members)
        {
            if (member is MetadataNamespace nsp)
            {
                AddTypes(receiver, nsp);
            }
            else
            {
                var type = (MetadataNamedType)member;
                AddTypes(receiver, type);
            }
        }
    }

    private static void AddTypes(ICollection<MetadataNamedType> receiver, MetadataNamedType type)
    {
        receiver.Add(type);
        foreach (var nestedType in type.GetNestedTypes())
            AddTypes(receiver, nestedType);
    }

    private ImmutableArray<MetadataExportedType> ComputeExportedTypes()
    {
        var exportedTypes = _metadataReader.ExportedTypes;
        var builder = ImmutableArray.CreateBuilder<MetadataExportedType>(exportedTypes.Count);

        foreach (var handle in exportedTypes)
        {
            var exportedType = new MetadataExportedType(this, handle);
            builder.Add(exportedType);
        }

        return builder.MoveToImmutable();
    }

    private ImmutableArray<MetadataCustomAttribute> ComputeCustomAttributes()
    {
        var definition = _metadataReader.GetModuleDefinition();
        var customAttributes = definition.GetCustomAttributes();
        return GetCustomAttributes(customAttributes);
    }

    private ImmutableArray<MetadataAssemblyReference> ComputeAssemblyReferences()
    {
        var assemblyReferences = _metadataReader.AssemblyReferences;
        var result = ImmutableArray.CreateBuilder<MetadataAssemblyReference>(assemblyReferences.Count);

        foreach (var assemblyReferenceHandle in assemblyReferences)
        {
            var metadataAssemblyReference = GetAssemblyReference(assemblyReferenceHandle);
            result.Add(metadataAssemblyReference);
        }

        return result.ToImmutable();
    }

    private ImmutableArray<MetadataFileReference> ComputeFileReferences()
    {
        var result = ImmutableArray.CreateBuilder<MetadataFileReference>(_metadataReader.AssemblyFiles.Count);

        foreach (var fileHandle in _metadataReader.AssemblyFiles)
        {
            var file = new MetadataFileReference(this, fileHandle);
            result.Add(file);
        }

        return result.MoveToImmutable();
    }

    private MetadataNamedTypeReference[] ComputeSpecialTypes()
    {
        const int numberElements = (int)MetadataSpecialType.System_Void + 1;
        var result = new MetadataNamedTypeReference?[numberElements];

        foreach (var typeReference in GetTypeReferences())
        {
            if (typeReference.NamespaceName == "System")
            {
                var specialType = SpecialNames.GetSpecialTypeForName(typeReference.Name);
                if (specialType is not null)
                    result[(int)specialType.Value] = typeReference;
            }
        }

        var systemObject = result[0];
        var corlib = systemObject is not null
                        ? systemObject.ContainingFile
                        : AssemblyReferences.Any()
                            ? AssemblyReferences.First()
                            : this;

        for (var i = 0; i < result.Length; i++)
        {
            var specialType = (MetadataSpecialType)i;
            var name = SpecialNames.GetNameForSpecialType(specialType);
            result[i] ??= new MetadataNamedTypeReferenceForName(corlib, "System", name);
        }

        return result!;
    }
}