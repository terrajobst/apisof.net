using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Arroyo.Signatures;

internal readonly struct MetadataTypeProvider : ISignatureTypeProvider<MetadataType, MetadataGenericContext>
{
    private readonly MetadataModule _module;

    public MetadataTypeProvider(MetadataModule module)
    {
        _module = module;
    }

    public MetadataModule Module
    {
        get { return _module; }
    }

    public MetadataType GetArrayType(MetadataType elementType, ArrayShape shape)
    {
        return new MetadataArrayTypeImpl(elementType, shape.Rank, shape.LowerBounds, shape.Sizes);
    }

    public MetadataType GetByReferenceType(MetadataType elementType)
    {
        return new MetadataByReferenceType(elementType);
    }

    public MetadataType GetFunctionPointerType(MethodSignature<MetadataType> signature)
    {
        return new MetadataFunctionPointerType(new MetadataSignature(signature));
    }

    public MetadataType GetGenericInstantiation(MetadataType genericType, ImmutableArray<MetadataType> typeArguments)
    {
        var genericTypeNamed = (MetadataNamedTypeReference)genericType;
        return new MetadataNamedTypeInstance(genericTypeNamed, typeArguments);
    }

    public MetadataType GetGenericMethodParameter(MetadataGenericContext genericContext, int index)
    {
        if (genericContext.GenericMethod is not null)
            return genericContext.GenericMethod.GenericParameters[index];
        else
            return new MetadataTypeParameterReferenceForIndex(isType: false, index);
    }

    public MetadataType GetGenericTypeParameter(MetadataGenericContext genericContext, int index)
    {
        if (genericContext.GenericType is not null)
            return genericContext.GenericType.GetAllGenericParameters()[index];
        else
            return new MetadataTypeParameterReferenceForIndex(isType: true, index);
    }

    public MetadataType GetModifiedType(MetadataType modifier, MetadataType unmodifiedType, bool isRequired)
    {
        var modifierType = (MetadataNamedTypeReference)modifier;
        var customModifier = new MetadataCustomModifier(modifierType, isRequired);
        return new MetadataModifiedType(unmodifiedType, customModifier);
    }

    public MetadataType GetPinnedType(MetadataType elementType)
    {
        return new MetadataPinnedType(elementType);
    }

    public MetadataType GetPointerType(MetadataType elementType)
    {
        return new MetadataPointerType(elementType);
    }

    public MetadataType GetPrimitiveType(PrimitiveTypeCode typeCode)
    {
        switch (typeCode)
        {
            case PrimitiveTypeCode.Void:
                return _module.GetSpecialType(MetadataSpecialType.System_Void);
            case PrimitiveTypeCode.Boolean:
                return _module.GetSpecialType(MetadataSpecialType.System_Boolean);
            case PrimitiveTypeCode.Char:
                return _module.GetSpecialType(MetadataSpecialType.System_Char);
            case PrimitiveTypeCode.SByte:
                return _module.GetSpecialType(MetadataSpecialType.System_SByte);
            case PrimitiveTypeCode.Byte:
                return _module.GetSpecialType(MetadataSpecialType.System_Byte);
            case PrimitiveTypeCode.Int16:
                return _module.GetSpecialType(MetadataSpecialType.System_Int16);
            case PrimitiveTypeCode.UInt16:
                return _module.GetSpecialType(MetadataSpecialType.System_UInt16);
            case PrimitiveTypeCode.Int32:
                return _module.GetSpecialType(MetadataSpecialType.System_Int32);
            case PrimitiveTypeCode.UInt32:
                return _module.GetSpecialType(MetadataSpecialType.System_UInt32);
            case PrimitiveTypeCode.Int64:
                return _module.GetSpecialType(MetadataSpecialType.System_Int64);
            case PrimitiveTypeCode.UInt64:
                return _module.GetSpecialType(MetadataSpecialType.System_UInt64);
            case PrimitiveTypeCode.Single:
                return _module.GetSpecialType(MetadataSpecialType.System_Single);
            case PrimitiveTypeCode.Double:
                return _module.GetSpecialType(MetadataSpecialType.System_Double);
            case PrimitiveTypeCode.String:
                return _module.GetSpecialType(MetadataSpecialType.System_String);
            case PrimitiveTypeCode.TypedReference:
                return _module.GetSpecialType(MetadataSpecialType.System_TypedReference);
            case PrimitiveTypeCode.IntPtr:
                return _module.GetSpecialType(MetadataSpecialType.System_IntPtr);
            case PrimitiveTypeCode.UIntPtr:
                return _module.GetSpecialType(MetadataSpecialType.System_UIntPtr);
            case PrimitiveTypeCode.Object:
                return _module.GetSpecialType(MetadataSpecialType.System_Object);
            default:
                throw new Exception($"Unexpected primitive type {typeCode}");
        }
    }

    public MetadataType GetSZArrayType(MetadataType elementType)
    {
        return new MetadataSZArrayType(elementType);
    }

    public MetadataType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        return _module.GetNamedType(handle)!;
    }

    public MetadataType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {
        return _module.GetNamedTypeReference(handle);
    }

    public MetadataType GetTypeFromSpecification(MetadataReader reader, MetadataGenericContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
    {
        var specification = reader.GetTypeSpecification(handle);
        return specification.DecodeSignature(this, genericContext);
    }
}