using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace Arroyo.Signatures;

internal readonly struct GuessingCustomAttributeTypeProvider : ICustomAttributeTypeProvider<MetadataType>
{
    private readonly MetadataTypeProvider _typeProvider;
    private readonly Dictionary<string, int> _enumSizes = new Dictionary<string, int>();

    public GuessingCustomAttributeTypeProvider(MetadataTypeProvider typeProvider)
    {
        _typeProvider = typeProvider;
    }

    public MetadataType GetPrimitiveType(PrimitiveTypeCode typeCode)
    {
        return _typeProvider.GetPrimitiveType(typeCode);
    }

    public MetadataType GetSZArrayType(MetadataType elementType)
    {
        return _typeProvider.GetSZArrayType(elementType);
    }

    public MetadataType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        return _typeProvider.GetTypeFromDefinition(reader, handle, rawTypeKind);
    }

    public MetadataType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        return _typeProvider.GetTypeFromReference(reader, handle, rawTypeKind);
    }

    public MetadataType GetSystemType()
    {
        return _typeProvider.Module.GetSpecialType(MetadataSpecialType.System_Type);
    }

    public MetadataType GetTypeFromSerializedName(string name)
    {
        var fullTypeName = AssemblyQualifiedTypeName.DecodeFromSerializedForm(name);
        return GetTypeFromSerializedName(fullTypeName);
    }

    private MetadataType GetTypeFromSerializedName(AssemblyQualifiedTypeName fullTypeName)
    {
        Debug.Assert(fullTypeName.TopLevelType is not null);

        var lastDot = fullTypeName.TopLevelType.LastIndexOf('.');
        var namespaceName = lastDot < 0
            ? ""
            : fullTypeName.TopLevelType[..lastDot];
        var typeName = lastDot < 0
            ? fullTypeName.TopLevelType
            : fullTypeName.TopLevelType[(lastDot + 1)..];
        var moduleFile = string.IsNullOrEmpty(fullTypeName.AssemblyName)
            ? (MetadataFile)_typeProvider.Module
            : new MetadataAssemblyReferenceForName(fullTypeName.AssemblyName);

        var namedType = (MetadataNamedTypeReference)new MetadataNamedTypeReferenceForName(moduleFile, namespaceName, typeName);

        if (fullTypeName.NestedTypes is not null)
        {
            foreach (var nested in fullTypeName.NestedTypes)
                namedType = new MetadataNamedTypeReferenceForNameNested(namedType, nested);
        }

        var result = (MetadataType)namedType;

        if (fullTypeName.TypeArguments is not null)
        {
            var arguments = ImmutableArray.CreateBuilder<MetadataType>(fullTypeName.TypeArguments.Length);
            foreach (var argumentName in fullTypeName.TypeArguments)
            {
                var type = GetTypeFromSerializedName(argumentName);
                arguments.Add(type);
            }
            result = new MetadataNamedTypeInstance(namedType, arguments.MoveToImmutable());
        }

        for (var i = 0; i < fullTypeName.PointerCount; i++)
            result = new MetadataPointerType(result);

        if (fullTypeName.ArrayRanks is not null)
        {
            if (fullTypeName.ArrayRanks.Length == 0)
            {
                result = new MetadataSZArrayType(result);
            }
            else
            {
                foreach (var rank in fullTypeName.ArrayRanks)
                    result = new MetadataArrayTypeImpl(result, rank, ImmutableArray<int>.Empty, ImmutableArray<int>.Empty);
            }
        }

        return result;
    }

    public bool IsSystemType(MetadataType type)
    {
        // NOTE: We aren't super precise here because for the special types we can't actually be sure which modules
        //       they came from as we don't resolve types.
        //
        //       In practical terms, if someone managed to construct an attribute with a constant typed
        //       as System.Type, it's gonna be THE System.Type and not some user defined type with that
        //       name, unless they built a custom compiler or the metadata is bogus. Since decoding
        //       custom attributes without resolving is always a guessing game anyways, just comparing
        //       name and namespace is fine and helps avoids false negatives where we constructed a
        //       System.Type with the wrong module name.

        var systemType = _typeProvider.Module.GetSpecialType(MetadataSpecialType.System_Type);
        return type is MetadataNamedTypeReference reference &&
               reference.Name == systemType.Name &&
               reference.NamespaceName == systemType.NamespaceName;
    }

    public PrimitiveTypeCode GetUnderlyingEnumType(MetadataType type)
    {
        // First, let's handle definitions.

        if (type is MetadataNamedType namedType)
        {
            var enumField = namedType.GetFields().FirstOrDefault(f => !f.IsStatic);
            if (enumField is not null &&
                enumField.FieldType is MetadataNamedTypeReference fieldType &&
                fieldType.NamespaceName == "System")
            {
                switch (fieldType.Name)
                {
                    case "SByte":
                        return PrimitiveTypeCode.SByte;
                    case "Int16":
                        return PrimitiveTypeCode.Int16;
                    case "Int32":
                        return PrimitiveTypeCode.Int32;
                    case "Int64":
                        return PrimitiveTypeCode.Int64;
                    case "Byte":
                        return PrimitiveTypeCode.Byte;
                    case "UInt16":
                        return PrimitiveTypeCode.UInt16;
                    case "UInt32":
                        return PrimitiveTypeCode.UInt32;
                    case "UInt64":
                        return PrimitiveTypeCode.UInt64;
                }
            }
        }

        // Must be a type reference then

        var typeReference = (MetadataNamedTypeReference)type;
        var fullName = typeReference.GetFullName();

        if (!_typeProvider.Module.CommittedEnumSizes.TryGetValue(fullName, out var size) &&
            !_enumSizes.TryGetValue(fullName, out size))
        {
            size = 4;
            _enumSizes.Add(fullName, size);
        }

        return ToTypeCode(size);
    }

    public bool TryNextPermutation()
    {
        var allPermutationsHaveBeenTried = true;

        foreach (var enumType in _enumSizes.Keys)
        {
            var oldSize = _enumSizes[enumType];
            if (oldSize == 4)
                _enumSizes[enumType] = 1;
            else if (oldSize == 1)
                _enumSizes[enumType] = 2;
            else if (oldSize == 2)
                _enumSizes[enumType] = 8;
            else
            {
                _enumSizes[enumType] = 4;
                continue;
            }
            allPermutationsHaveBeenTried = false;
            break;
        }

        return !allPermutationsHaveBeenTried;
    }

    public void Commit()
    {
        foreach (var (type, size) in _enumSizes)
            _typeProvider.Module.CommittedEnumSizes.TryAdd(type, size);
    }

    private static PrimitiveTypeCode ToTypeCode(int size)
    {
        switch (size)
        {
            case 1: return PrimitiveTypeCode.Byte;
            case 2: return PrimitiveTypeCode.UInt16;
            case 4: return PrimitiveTypeCode.UInt32;
            case 8: return PrimitiveTypeCode.UInt64;
            default:
                throw new Exception($"Unexpected size: {size}");
        }
    }
}