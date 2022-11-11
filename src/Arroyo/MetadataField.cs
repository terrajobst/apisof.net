using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Arroyo.Signatures;

namespace Arroyo;

public sealed class MetadataField : MetadataFieldReference, IMetadataTypeMember
{
    private readonly MetadataNamedType _containingType;
    private readonly FieldDefinitionHandle _handle;
    private readonly FieldAttributes _attributes;

    private string? _name;
    private MetadataType? _fieldType;
    private bool _isVolatile;
    private bool _isRequired;
    private MetadataRefKind _refKind;
    private int? _fixedSizeBuffer;
    private ImmutableArray<MetadataCustomModifier> _customModifiers;
    private ImmutableArray<MetadataCustomAttribute> _customAttributes;
    private ImmutableArray<MetadataCustomModifier> _refCustomModifiers;
    private MetadataConstant? _defaultValue;

    internal MetadataField(MetadataNamedType containingType, FieldDefinitionHandle handle)
    {
        _containingType = containingType;
        _handle = handle;
        var definition = containingType.ContainingModule.MetadataReader.GetFieldDefinition(handle);
        _attributes = definition.Attributes;

        // TODO: _definition.GetMarshallingDescriptor()
        // TODO: _definition.GetOffset()
        // TODO: _definition.GetRelativeVirtualAddress()
    }

    public override int Token
    {
        get { return MetadataTokens.GetToken(_handle); }
    }

    private protected override MetadataType ContainingTypeCore
    {
        get { return _containingType; }
    }

    public new MetadataNamedType ContainingType
    {
        get { return _containingType; }
    }

    public bool IsPrivateScope
    {
        get { return _attributes.IsFlagSet(FieldAttributes.PrivateScope); }
    }

    public bool IsStatic
    {
        get { return _attributes.IsFlagSet(FieldAttributes.Static); }
    }

    public bool IsInitOnly
    {
        get { return _attributes.IsFlagSet(FieldAttributes.InitOnly); }
    }

    public bool IsLiteral
    {
        get { return _attributes.IsFlagSet(FieldAttributes.Literal); }
    }

    public bool IsNotSerialized
    {
        get { return _attributes.IsFlagSet(FieldAttributes.NotSerialized); }
    }

    public bool IsSpecialName
    {
        get { return _attributes.IsFlagSet(FieldAttributes.SpecialName); }
    }

    public bool IsPinvokeImplementation
    {
        get { return _attributes.IsFlagSet(FieldAttributes.PinvokeImpl); }
    }

    public bool IsRuntimeSpecialName
    {
        get { return _attributes.IsFlagSet(FieldAttributes.RTSpecialName); }
    }

    public bool HasMarshallingInformation
    {
        get { return _attributes.IsFlagSet(FieldAttributes.HasFieldMarshal); }
    }

    public bool HasFieldRva
    {
        get { return _attributes.IsFlagSet(FieldAttributes.HasFieldRVA); }
    }

    public bool HasDefault
    {
        get { return _attributes.IsFlagSet(FieldAttributes.HasDefault); }
    }

    public MetadataConstant? DefaultValue
    {
        get
        {
            if (HasDefault && _defaultValue is null)
            {
                var defaultValue = ComputeDefaultValue();
                Interlocked.CompareExchange(ref _defaultValue, defaultValue, null);
            }

            return _defaultValue;
        }
    }

    public MetadataAccessibility Accessibility
    {
        get { return _attributes.GetAccessibility(); }
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

    public override MetadataType FieldType
    {
        get
        {
            EnsureSignatureLoaded();
            return _fieldType;
        }
    }

    public bool IsVolatile
    {
        get
        {
            EnsureSignatureLoaded();
            return _isVolatile;
        }
    }

    public bool IsRequired
    {
        get
        {
            EnsureSignatureLoaded();
            return _isRequired;
        }
    }

    public MetadataRefKind RefKind
    {
        get
        {
            EnsureSignatureLoaded();
            return _refKind;
        }
    }

    public int? FixedSizeBuffer
    {
        get
        {
            EnsureSignatureLoaded();
            return _fixedSizeBuffer;
        }
    }

    public ImmutableArray<MetadataCustomModifier> CustomModifiers
    {
        get
        {
            EnsureSignatureLoaded();
            return _customModifiers;
        }
    }

    public ImmutableArray<MetadataCustomModifier> RefCustomModifiers
    {
        get
        {
            EnsureSignatureLoaded();
            return _refCustomModifiers;
        }
    }

    public override MetadataCustomAttributeEnumerator GetCustomAttributes(bool includeProcessed = false)
    {
        EnsureSignatureLoaded();
        return new MetadataCustomAttributeEnumerator(_customAttributes, includeProcessed);
    }

    private MetadataConstant ComputeDefaultValue()
    {
        var definition = _containingType.ContainingModule.MetadataReader.GetFieldDefinition(_handle);
        var constantHandle = definition.GetDefaultValue();
        return new MetadataConstant(_containingType.ContainingModule, constantHandle);
    }

    private string ComputeName()
    {
        var definition = _containingType.ContainingModule.MetadataReader.GetFieldDefinition(_handle);
        return _containingType.ContainingModule.MetadataReader.GetString(definition.Name);
    }

    private MetadataType ComputeFieldType()
    {
        var definition = _containingType.ContainingModule.MetadataReader.GetFieldDefinition(_handle);
        var genericContext = new MetadataGenericContext(_containingType);
        return definition.DecodeSignature(_containingType.ContainingModule.TypeProvider, genericContext);
    }

    private ImmutableArray<MetadataCustomAttribute> ComputeCustomAttributes()
    {
        var definition = _containingType.ContainingModule.MetadataReader.GetFieldDefinition(_handle);
        var customAttributes = definition.GetCustomAttributes();
        return _containingType.ContainingModule.GetCustomAttributes(customAttributes);
    }

    [MemberNotNull(nameof(_fieldType))]
    private void EnsureSignatureLoaded()
    {
        if (_fieldType is not null)
            return;

        var customAttributes = ComputeCustomAttributes();
        var fieldType = ComputeFieldType();

        MetadataTypeProcessor.Process(ref fieldType,
                                      out var isByRef,
                                      out var customModifiers,
                                      out var refCustomModifiers);

        var refKind = isByRef ? MetadataRefKind.Ref : MetadataRefKind.None;
        var isVolatile = false;
        var isRequired = false;
        var fixedSizeBuffer = (int?)null;

        foreach (var customAttribute in customAttributes)
        {
            if (customAttribute.Constructor.ContainingType is MetadataNamedTypeReference attributeType)
            {
                if (attributeType.Name == "IsReadOnlyAttribute" &&
                    attributeType.NamespaceName == "System.Runtime.CompilerServices" &&
                    customAttribute.FixedArguments.Length == 0 &&
                    customAttribute.NamedArguments.Length == 0)
                {
                    if (refKind != MetadataRefKind.None)
                    {
                        refKind = MetadataRefKind.In;
                        customAttribute.MarkProcessed();
                    }
                }
                else if (attributeType.Name == "RequiredMemberAttribute" &&
                         attributeType.NamespaceName == "System.Runtime.CompilerServices" &&
                         customAttribute.FixedArguments.Length == 0 &&
                         customAttribute.NamedArguments.Length == 0)
                {
                    isRequired = true;
                    customAttribute.MarkProcessed();
                }
                else if (attributeType.Name == "FixedBufferAttribute" &&
                         attributeType.NamespaceName == "System.Runtime.CompilerServices" &&
                         customAttribute.FixedArguments.Length == 2 &&
                         customAttribute.NamedArguments.Length == 0 &&
                         customAttribute.FixedArguments[0].Value is MetadataType elementType &&
                         customAttribute.FixedArguments[1].Value is int length)
                {
                    fixedSizeBuffer = length;
                    fieldType = _containingType.ContainingModule.TypeProvider.GetPointerType(elementType);
                    customAttribute.MarkProcessed();
                }
            }
        }

        foreach (var customModifier in customModifiers)
        {
            if (customModifier.Type.Name == "IsVolatile" &&
                customModifier.Type.NamespaceName == "System.Runtime.CompilerServices")
            {
                isVolatile = true;
                break;
            }
        }

        ImmutableInterlocked.InterlockedInitialize(ref _customAttributes, customAttributes);
        ImmutableInterlocked.InterlockedInitialize(ref _customModifiers, customModifiers);
        ImmutableInterlocked.InterlockedInitialize(ref _refCustomModifiers, refCustomModifiers);
        _refKind = refKind;
        _isRequired = isRequired;
        _isVolatile = isVolatile;
        _fixedSizeBuffer = fixedSizeBuffer;

        Interlocked.CompareExchange(ref _fieldType, fieldType, null);
    }
}