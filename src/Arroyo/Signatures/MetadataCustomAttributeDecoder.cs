using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;

namespace Arroyo.Signatures;

// NOTE: This was copied from SRM and adjusted to avoid throwing exceptions in order
//       to make the guessing game for enum types viable performance wise.

internal readonly struct MetadataCustomAttributeDecoder<TType>
{
    private readonly ICustomAttributeTypeProvider<TType> _provider;
    private readonly MetadataReader _reader;

    public MetadataCustomAttributeDecoder(ICustomAttributeTypeProvider<TType> provider, MetadataReader reader)
    {
        _reader = reader;
        _provider = provider;
    }

    public bool TryDecodeValue(EntityHandle constructor, BlobHandle value, out CustomAttributeValue<TType> result)
    {
        BlobHandle signature;
        BlobHandle attributeOwningTypeSpec = default;
        switch (constructor.Kind)
        {
            case HandleKind.MethodDefinition:
                var definition = _reader.GetMethodDefinition((MethodDefinitionHandle)constructor);
                signature = definition.Signature;
                break;

            case HandleKind.MemberReference:
                var reference = _reader.GetMemberReference((MemberReferenceHandle)constructor);
                signature = reference.Signature;

                // If this is a generic attribute, we'll need its instantiation to decode the signatures
                if (reference.Parent.Kind == HandleKind.TypeSpecification)
                {
                    var genericOwner = _reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent);
                    attributeOwningTypeSpec = genericOwner.Signature;
                }
                break;

            default:
                result = default;
                return false;
        }

        var signatureReader = _reader.GetBlobReader(signature);
        var valueReader = _reader.GetBlobReader(value);

        var prolog = valueReader.ReadUInt16();
        if (prolog != 1)
        {
            result = default;
            return false;
        }

        var header = signatureReader.ReadSignatureHeader();
        if (header.Kind != SignatureKind.Method || header.IsGeneric)
        {
            result = default;
            return false;
        }

        var parameterCount = signatureReader.ReadCompressedInteger();
        var returnType = signatureReader.ReadSignatureTypeCode();
        if (returnType != SignatureTypeCode.Void)
        {
            result = default;
            return false;
        }

        BlobReader genericContextReader = default;
        if (!attributeOwningTypeSpec.IsNil)
        {
            // If this is a generic attribute, grab the instantiation arguments so that we can
            // interpret the constructor signature, should it refer to the generic context.
            genericContextReader = _reader.GetBlobReader(attributeOwningTypeSpec);
            if (genericContextReader.ReadSignatureTypeCode() == SignatureTypeCode.GenericTypeInstance)
            {
                var kind = genericContextReader.ReadCompressedInteger();
                if (kind != (int)SignatureTypeKind.Class && kind != (int)SignatureTypeKind.ValueType)
                {
                    result = default;
                    return false;
                }

                genericContextReader.ReadTypeHandle();

                // At this point, the reader points to the "GenArgCount Type Type*" part of the signature.
            }
            else
            {
                // Some other invalid TypeSpec. Don't accidentally allow resolving generic parameters
                // from the constructor signature into a broken blob.
                genericContextReader = default;
            }
        }


        if (!TryDecodeFixedArguments(ref signatureReader, ref valueReader, parameterCount, genericContextReader, out var fixedArguments) ||
            !TryDecodeNamedArguments(ref valueReader, out var namedArguments))
        {
            result = default;
            return false;
        }

        if (signatureReader.RemainingBytes > 0 ||
            valueReader.RemainingBytes > 0)
        {
            result = default;
            return false;
        }

        result = new CustomAttributeValue<TType>(fixedArguments, namedArguments);
        return true;
    }

    private bool TryDecodeFixedArguments(ref BlobReader signatureReader, ref BlobReader valueReader, int count, BlobReader genericContextReader, out ImmutableArray<CustomAttributeTypedArgument<TType>> result)
    {
        if (count == 0)
        {
            result = ImmutableArray<CustomAttributeTypedArgument<TType>>.Empty;
            return true;
        }

        var arguments = ImmutableArray.CreateBuilder<CustomAttributeTypedArgument<TType>>(count);

        for (var i = 0; i < count; i++)
        {
            if (!TryDecodeFixedArgumentType(ref signatureReader, genericContextReader, out var info))
            {
                result = default;
                return false;
            }

            if (!TryDecodeArgument(ref valueReader, info, out var argument))
            {
                result = default;
                return false;
            }

            arguments.Add(argument);
        }

        result = arguments.MoveToImmutable();
        return true;
    }

    private bool TryDecodeNamedArguments(ref BlobReader valueReader, out ImmutableArray<CustomAttributeNamedArgument<TType>> result)
    {
        if (!valueReader.TryReadUInt16(out var count))
        {
            result = default;
            return false;
        }

        if (count == 0)
        {
            result = ImmutableArray<CustomAttributeNamedArgument<TType>>.Empty;
            return true;
        }

        var arguments = ImmutableArray.CreateBuilder<CustomAttributeNamedArgument<TType>>(count);
        for (var i = 0; i < count; i++)
        {
            var kind = (CustomAttributeNamedArgumentKind)valueReader.ReadSerializationTypeCode();
            if (kind != CustomAttributeNamedArgumentKind.Field && kind != CustomAttributeNamedArgumentKind.Property)
            {
                result = default;
                return false;
            }

            if (!TryDecodeNamedArgumentType(ref valueReader, out var info))
            {
                result = default;
                return false;
            }

            if (!valueReader.TryReadSerializedString(out var name))
            {
                result = default;
                return false;
            }

            if (!TryDecodeArgument(ref valueReader, info, out var argument))
            {
                result = default;
                return false;
            }

            arguments.Add(new CustomAttributeNamedArgument<TType>(name, kind, argument.Type, argument.Value));
        }

        result = arguments.MoveToImmutable();
        return true;
    }

    private struct ArgumentTypeInfo
    {
        public TType Type;
        public TType ElementType;
        public SerializationTypeCode TypeCode;
        public SerializationTypeCode ElementTypeCode;
    }

    private bool TryDecodeFixedArgumentType(ref BlobReader signatureReader, BlobReader genericContextReader, out ArgumentTypeInfo result, bool isElementType = false)
    {
        var signatureTypeCode = signatureReader.ReadSignatureTypeCode();

        var info = new ArgumentTypeInfo
        {
            TypeCode = (SerializationTypeCode)signatureTypeCode,
        };

        switch (signatureTypeCode)
        {
            case SignatureTypeCode.Boolean:
            case SignatureTypeCode.Byte:
            case SignatureTypeCode.Char:
            case SignatureTypeCode.Double:
            case SignatureTypeCode.Int16:
            case SignatureTypeCode.Int32:
            case SignatureTypeCode.Int64:
            case SignatureTypeCode.SByte:
            case SignatureTypeCode.Single:
            case SignatureTypeCode.String:
            case SignatureTypeCode.UInt16:
            case SignatureTypeCode.UInt32:
            case SignatureTypeCode.UInt64:
                info.Type = _provider.GetPrimitiveType((PrimitiveTypeCode)signatureTypeCode);
                break;

            case SignatureTypeCode.Object:
                info.TypeCode = SerializationTypeCode.TaggedObject;
                info.Type = _provider.GetPrimitiveType(PrimitiveTypeCode.Object);
                break;

            case SignatureTypeCode.TypeHandle:
                // Parameter is type def or ref and is only allowed to be System.Type or Enum.
                var handle = signatureReader.ReadTypeHandle();

                if (!TryGetTypeFromHandle(handle, out var type))
                {
                    result = default;
                    return false;
                }

                info.Type = type;
                info.TypeCode = _provider.IsSystemType(info.Type) ? SerializationTypeCode.Type : (SerializationTypeCode)_provider.GetUnderlyingEnumType(info.Type);
                break;

            case SignatureTypeCode.SZArray:
                if (isElementType)
                {
                    // jagged arrays are not allowed.
                    result = default;
                    return false;
                }

                if (!TryDecodeFixedArgumentType(ref signatureReader, genericContextReader, out var elementInfo, isElementType: true))
                {
                    result = default;
                    return false;
                }

                info.ElementType = elementInfo.Type;
                info.ElementTypeCode = elementInfo.TypeCode;
                info.Type = _provider.GetSZArrayType(info.ElementType);
                break;

            case SignatureTypeCode.GenericTypeParameter:
                if (genericContextReader.Length == 0)
                {
                    result = default;
                    return false;
                }

                var parameterIndex = signatureReader.ReadCompressedInteger();
                var numGenericParameters = genericContextReader.ReadCompressedInteger();
                if (parameterIndex >= numGenericParameters)
                {
                    result = default;
                    return false;
                }

                while (parameterIndex > 0)
                {
                    if (!TrySkipType(ref genericContextReader))
                    {
                        result = default;
                        return false;
                    }
                    parameterIndex--;
                }

                return TryDecodeFixedArgumentType(ref genericContextReader, default, out result, isElementType);

            default:
                result = default;
                return false;
        }

        result = info;
        return true;
    }

    private bool TryDecodeNamedArgumentType(ref BlobReader valueReader, out ArgumentTypeInfo result, bool isElementType = false)
    {
        var info = new ArgumentTypeInfo
        {
            TypeCode = valueReader.ReadSerializationTypeCode(),
        };

        switch (info.TypeCode)
        {
            case SerializationTypeCode.Boolean:
            case SerializationTypeCode.Byte:
            case SerializationTypeCode.Char:
            case SerializationTypeCode.Double:
            case SerializationTypeCode.Int16:
            case SerializationTypeCode.Int32:
            case SerializationTypeCode.Int64:
            case SerializationTypeCode.SByte:
            case SerializationTypeCode.Single:
            case SerializationTypeCode.String:
            case SerializationTypeCode.UInt16:
            case SerializationTypeCode.UInt32:
            case SerializationTypeCode.UInt64:
                info.Type = _provider.GetPrimitiveType((PrimitiveTypeCode)info.TypeCode);
                break;

            case SerializationTypeCode.Type:
                info.Type = _provider.GetSystemType();
                break;

            case SerializationTypeCode.TaggedObject:
                info.Type = _provider.GetPrimitiveType(PrimitiveTypeCode.Object);
                break;

            case SerializationTypeCode.SZArray:
                if (isElementType)
                {
                    // jagged arrays are not allowed.
                    result = default;
                    return false;
                }

                if (!TryDecodeNamedArgumentType(ref valueReader, out var elementInfo, isElementType: true))
                {
                    result = default;
                    return false;
                }

                info.ElementType = elementInfo.Type;
                info.ElementTypeCode = elementInfo.TypeCode;
                info.Type = _provider.GetSZArrayType(info.ElementType);
                break;

            case SerializationTypeCode.Enum:
                if (!valueReader.TryReadSerializedString(out var typeName) ||
                    typeName is null)
                {
                    result = default;
                    return false;
                }
                info.Type = _provider.GetTypeFromSerializedName(typeName!);
                info.TypeCode = (SerializationTypeCode)_provider.GetUnderlyingEnumType(info.Type);
                break;

            default:
                result = default;
                return false;
        }

        result = info;
        return true;
    }

    private bool TryDecodeArgument(ref BlobReader valueReader, ArgumentTypeInfo info, out CustomAttributeTypedArgument<TType> result)
    {
        if (info.TypeCode == SerializationTypeCode.TaggedObject)
        {
            if (!TryDecodeNamedArgumentType(ref valueReader, out info))
            {
                result = default;
                return false;
            }
        }

        // TODO: Guard other cases
        //
        // PERF_TODO: https://github.com/dotnet/runtime/issues/16551
        //   Cache /reuse common arguments to avoid boxing (small integers, true, false).
        object? value;
        switch (info.TypeCode)
        {
            case SerializationTypeCode.Boolean:
                value = valueReader.ReadBoolean();
                break;

            case SerializationTypeCode.Byte:
                if (!valueReader.TryReadByte(out var valueByte))
                {
                    result = default;
                    return false;
                }

                value = valueByte;
                break;

            case SerializationTypeCode.Char:
                value = valueReader.ReadChar();
                break;

            case SerializationTypeCode.Double:
                value = valueReader.ReadDouble();
                break;

            case SerializationTypeCode.Int16:
                value = valueReader.ReadInt16();
                break;

            case SerializationTypeCode.Int32:
                value = valueReader.ReadInt32();
                break;

            case SerializationTypeCode.Int64:
                value = valueReader.ReadInt64();
                break;

            case SerializationTypeCode.SByte:
                value = valueReader.ReadSByte();
                break;

            case SerializationTypeCode.Single:
                value = valueReader.ReadSingle();
                break;

            case SerializationTypeCode.UInt16:
                if (!valueReader.TryReadUInt16(out var valueUInt16))
                {
                    result = default;
                    return false;
                }
                value = valueUInt16;
                break;

            case SerializationTypeCode.UInt32:
                if (!valueReader.TryReadUInt32(out var valueUInt32))
                {
                    result = default;
                    return false;
                }
                value = valueUInt32;
                break;

            case SerializationTypeCode.UInt64:
                if (!valueReader.TryReadUInt64(out var valueUInt64))
                {
                    result = default;
                    return false;
                }
                value = valueUInt64;
                break;

            case SerializationTypeCode.String:
                value = valueReader.ReadSerializedString();
                break;

            case SerializationTypeCode.Type:
                var typeName = valueReader.ReadSerializedString();
                if (typeName is null)
                {
                    result = default;
                    return false;
                }
                value = _provider.GetTypeFromSerializedName(typeName);
                break;

            case SerializationTypeCode.SZArray:
                if (TryDecodeArrayArgument(ref valueReader, info, out var arrayArgument))
                {
                    value = arrayArgument;
                }
                else
                {
                    result = default;
                    return false;
                }
                break;

            default:
                result = default;
                return false;
        }

        result = new CustomAttributeTypedArgument<TType>(info.Type, value);
        return true;
    }

    private bool TryDecodeArrayArgument(ref BlobReader blobReader, ArgumentTypeInfo info, out ImmutableArray<CustomAttributeTypedArgument<TType>>? result)
    {
        var count = blobReader.ReadInt32();
        if (count == -1)
        {
            result = null;
            return true;
        }

        if (count == 0)
        {
            result = ImmutableArray<CustomAttributeTypedArgument<TType>>.Empty;
            return true;
        }

        if (count < 0)
        {
            result = default;
            return false;
        }

        var elementInfo = new ArgumentTypeInfo
        {
            Type = info.ElementType,
            TypeCode = info.ElementTypeCode,
        };

        var array = ImmutableArray.CreateBuilder<CustomAttributeTypedArgument<TType>>(count);

        for (var i = 0; i < count; i++)
        {
            if (TryDecodeArgument(ref blobReader, elementInfo, out var arg))
            {
                array.Add(arg);
            }
            else
            {
                result = default;
                return false;
            }
        }

        result = array.MoveToImmutable();
        return true;
    }

    private bool TryGetTypeFromHandle(EntityHandle handle, [MaybeNullWhen(false)] out TType result)
    {
        switch (handle.Kind)
        {
            case HandleKind.TypeDefinition:
                result = _provider.GetTypeFromDefinition(_reader, (TypeDefinitionHandle)handle, 0);
                return true;
            case HandleKind.TypeReference:
                result = _provider.GetTypeFromReference(_reader, (TypeReferenceHandle)handle, 0);
                return true;
            default:
                result = default;
                return false;
        }
    }

    private static bool TrySkipType(ref BlobReader blobReader)
    {
        var typeCode = blobReader.ReadCompressedInteger();

        switch (typeCode)
        {
            case (int)SignatureTypeCode.Boolean:
            case (int)SignatureTypeCode.Char:
            case (int)SignatureTypeCode.SByte:
            case (int)SignatureTypeCode.Byte:
            case (int)SignatureTypeCode.Int16:
            case (int)SignatureTypeCode.UInt16:
            case (int)SignatureTypeCode.Int32:
            case (int)SignatureTypeCode.UInt32:
            case (int)SignatureTypeCode.Int64:
            case (int)SignatureTypeCode.UInt64:
            case (int)SignatureTypeCode.Single:
            case (int)SignatureTypeCode.Double:
            case (int)SignatureTypeCode.IntPtr:
            case (int)SignatureTypeCode.UIntPtr:
            case (int)SignatureTypeCode.Object:
            case (int)SignatureTypeCode.String:
            case (int)SignatureTypeCode.Void:
            case (int)SignatureTypeCode.TypedReference:
                return true;

            case (int)SignatureTypeCode.Pointer:
            case (int)SignatureTypeCode.ByReference:
            case (int)SignatureTypeCode.Pinned:
            case (int)SignatureTypeCode.SZArray:
                return TrySkipType(ref blobReader);

            case (int)SignatureTypeCode.FunctionPointer:
                var header = blobReader.ReadSignatureHeader();
                if (header.IsGeneric)
                {
                    blobReader.ReadCompressedInteger(); // arity
                }

                var paramCount = blobReader.ReadCompressedInteger();
                if (!TrySkipType(ref blobReader))
                    return false;
                for (var i = 0; i < paramCount; i++)
                    if (!TrySkipType(ref blobReader))
                        return false;
                return true;

            case (int)SignatureTypeCode.Array:
                if (!TrySkipType(ref blobReader))
                    return false;
                blobReader.ReadCompressedInteger(); // rank
                var boundsCount = blobReader.ReadCompressedInteger();
                for (var i = 0; i < boundsCount; i++)
                {
                    blobReader.ReadCompressedInteger();
                }
                var lowerBoundsCount = blobReader.ReadCompressedInteger();
                for (var i = 0; i < lowerBoundsCount; i++)
                {
                    blobReader.ReadCompressedSignedInteger();
                }
                return true;

            case (int)SignatureTypeCode.RequiredModifier:
            case (int)SignatureTypeCode.OptionalModifier:
                blobReader.ReadTypeHandle();
                if (!TrySkipType(ref blobReader))
                    return false;
                return true;

            case (int)SignatureTypeCode.GenericTypeInstance:
                if (!TrySkipType(ref blobReader))
                    return false;
                var count = blobReader.ReadCompressedInteger();
                for (var i = 0; i < count; i++)
                {
                    if (!TrySkipType(ref blobReader))
                        return false;
                }
                return true;

            case (int)SignatureTypeCode.GenericTypeParameter:
                blobReader.ReadCompressedInteger();
                return true;

            case (int)SignatureTypeKind.Class:
            case (int)SignatureTypeKind.ValueType:
                return TrySkipType(ref blobReader);

            default:
                return false;
        }
    }
}

internal static class DefensiveBlobReader
{
    public static bool TryReadByte(this ref BlobReader reader, out byte result)
    {
        if (reader.RemainingBytes < 1)
        {
            result = 0;
            return false;
        }

        result = reader.ReadByte();
        return true;
    }

    public static bool TryReadUInt16(this ref BlobReader reader, out ushort result)
    {
        if (reader.RemainingBytes < 2)
        {
            result = 0;
            return false;
        }

        result = reader.ReadUInt16();
        return true;
    }

    public static bool TryReadUInt32(this ref BlobReader reader, out uint result)
    {
        if (reader.RemainingBytes < 4)
        {
            result = 0;
            return false;
        }

        result = reader.ReadUInt32();
        return true;
    }

    public static bool TryReadUInt64(this ref BlobReader reader, out ulong result)
    {
        if (reader.RemainingBytes < 8)
        {
            result = 0;
            return false;
        }

        result = reader.ReadUInt64();
        return true;
    }

    public static bool TryReadSerializedString(this ref BlobReader reader, out string? result)
    {
        if (reader.TryReadCompressedInteger(out var length))
        {
            if (reader.RemainingBytes < length)
            {
                result = default;
                return false;
            }

            result = reader.ReadUTF8(length);
            return true;
        }
        else if (reader.TryReadByte(out var end))
        {
            if (end != 0xFF)
            {
                result = default;
                return false;
            }

            result = null;
            return true;
        }
        else
        {
            result = default;
            return true;
        }
    }
}