using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Arroyo.Signatures;

namespace Arroyo;

public sealed class MetadataProperty : MetadataMember, IMetadataTypeMember
{
    private readonly MetadataNamedType _containingType;
    private readonly PropertyDefinitionHandle _handle;
    private readonly PropertyAttributes _attributes;
    private readonly MetadataMethod? _getter;
    private readonly MetadataMethod? _setter;

    private string? _name;
    private MetadataAccessibility _accessibility = MetadataExtensions.UninitializedAccessibility;
    private bool _isRequired;
    private MetadataRefKind _refKind;
    private MetadataSignature? _signature;
    private ImmutableArray<MetadataParameter> _parameters;
    private ImmutableArray<MetadataCustomAttribute> _customAttributes;

    private MetadataConstant? _defaultValue;


    internal MetadataProperty(MetadataNamedType containingType,
                              PropertyDefinitionHandle handle,
                              Dictionary<int, MetadataMethod> methodByToken)
    {
        _containingType = containingType;
        _handle = handle;
        var definition = containingType.ContainingModule.MetadataReader.GetPropertyDefinition(handle);
        _attributes = definition.Attributes;

        var accessors = definition.GetAccessors();

        if (!accessors.Getter.IsNil)
        {
            var token = MetadataTokens.GetToken(accessors.Getter);
            methodByToken.TryGetValue(token, out _getter);
            _getter?.SetAssociatedMember(this);
        }

        if (!accessors.Setter.IsNil)
        {
            var token = MetadataTokens.GetToken(accessors.Setter);
            methodByToken.TryGetValue(token, out _setter);
            _setter?.SetAssociatedMember(this);
        }
    }

    public override int Token
    {
        get { return MetadataTokens.GetToken(_handle); }
    }

    private protected override MetadataType ContainingTypeCore
    {
        get { return _containingType; }
    }

    public bool IsSpecialName
    {
        get { return _attributes.IsFlagSet(PropertyAttributes.SpecialName); }
    }

    public bool IsRuntimeSpecialName
    {
        get { return _attributes.IsFlagSet(PropertyAttributes.RTSpecialName); }
    }

    public bool HasDefault
    {
        get { return _attributes.IsFlagSet(PropertyAttributes.HasDefault); }
    }

    public new MetadataNamedType ContainingType
    {
        get { return _containingType; }
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

    public MetadataMethod? Getter
    {
        get { return _getter; }
    }

    public MetadataMethod? Setter
    {
        get { return _setter; }
    }

    public IEnumerable<MetadataMethod> Accessors
    {
        get
        {
            if (_getter is not null)
                yield return _getter;

            if (_setter is not null)
                yield return _setter;
        }
    }

    public MetadataAccessibility Accessibility
    {
        get
        {
            if (_accessibility == MetadataExtensions.UninitializedAccessibility)
                _accessibility = MetadataExtensions.GetAccessibilityFromAccessors(_getter, _setter);

            return _accessibility;
        }
    }

    public bool IsStatic
    {
        get
        {
            return (_getter is null || _getter.IsStatic) &&
                   (_setter is null || _setter.IsStatic);
        }
    }

    public bool IsAbstract
    {
        get
        {
            return _getter is not null && _getter.IsAbstract ||
                   _setter is not null && _setter.IsAbstract;
        }
    }

    public bool IsVirtual
    {
        get
        {
            return !IsOverride && !IsAbstract &&
                   (_getter is not null && _getter.IsVirtual ||
                    _setter is not null && _setter.IsVirtual);
        }
    }

    public bool IsOverride
    {
        get
        {
            return _getter is not null && _getter.IsOverride ||
                   _setter is not null && _setter.IsOverride;
        }
    }

    public bool IsSealed
    {
        get
        {
            return (_getter is null || _getter.IsExtern) &&
                   (_setter is null || _setter.IsExtern);
        }
    }

    public bool IsExtern
    {
        get
        {
            return _getter is not null && _getter.IsExtern ||
                   _setter is not null && _setter.IsExtern;
        }
    }

    public ImmutableArray<MetadataParameter> Parameters
    {
        get
        {
            EnsureSignatureLoaded();
            return _parameters;
        }
    }

    public MetadataSignature Signature
    {
        get
        {
            EnsureSignatureLoaded();
            return _signature;
        }
    }

    public MetadataType PropertyType
    {
        get
        {
            EnsureSignatureLoaded();
            return _signature.ReturnType;
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

    public ImmutableArray<MetadataCustomModifier> CustomModifiers
    {
        get
        {
            EnsureSignatureLoaded();
            return _signature.ReturnTypeCustomModifiers;
        }
    }

    public ImmutableArray<MetadataCustomModifier> RefCustomModifiers
    {
        get
        {
            EnsureSignatureLoaded();
            return _signature.RefCustomModifiers;
        }
    }

    public MetadataCustomAttributeEnumerator GetCustomAttributes(bool includeProcessed = false)
    {
        EnsureSignatureLoaded();
        return new MetadataCustomAttributeEnumerator(_customAttributes, includeProcessed);
    }

    private string ComputeName()
    {
        var definition = _containingType.ContainingModule.MetadataReader.GetPropertyDefinition(_handle);
        return _containingType.ContainingModule.MetadataReader.GetString(definition.Name);
    }

    private ImmutableArray<MetadataParameter> ComputeParameters(MetadataSignature signature)
    {
        var parameterCount = signature.Parameters.Length;
        var method = _getter ?? _setter!;

        var result = ImmutableArray.CreateBuilder<MetadataParameter>(parameterCount);

        for (var i = 0; i < parameterCount; i++)
            result.Add(method.Parameters[i]);

        return result.ToImmutable();
    }

    private MetadataSignature ComputeSignature()
    {
        var definition = _containingType.ContainingModule.MetadataReader.GetPropertyDefinition(_handle);
        var genericContext = new MetadataGenericContext(ContainingType);
        return ContainingType.ContainingModule.GetMethodSignature(definition.Signature, genericContext);
    }

    private MetadataConstant ComputeDefaultValue()
    {
        var definition = _containingType.ContainingModule.MetadataReader.GetPropertyDefinition(_handle);
        var constantHandle = definition.GetDefaultValue();
        return new MetadataConstant(_containingType.ContainingModule, constantHandle);
    }

    private ImmutableArray<MetadataCustomAttribute> ComputeCustomAttributes()
    {
        var definition = _containingType.ContainingModule.MetadataReader.GetPropertyDefinition(_handle);
        var customAttributes = definition.GetCustomAttributes();
        return _containingType.ContainingModule.GetCustomAttributes(customAttributes);
    }

    [MemberNotNull(nameof(_signature))]
    private void EnsureSignatureLoaded()
    {
        if (_signature is not null)
        {
            Debug.Assert(_signature != null);
            return;
        }

        var customAttributes = ComputeCustomAttributes();
        var signature = ComputeSignature();
        var parameters = ComputeParameters(signature);

        var refKind = signature.ReturnsByRef ? MetadataRefKind.Ref : MetadataRefKind.None;
        var isRequired = false;

        foreach (var customAttribute in customAttributes)
        {
            if (customAttribute.FixedArguments.Length == 0 &&
                customAttribute.NamedArguments.Length == 0 &&
                customAttribute.Constructor.ContainingType is MetadataNamedTypeReference attributeType)
            {
                if (attributeType.Name == "IsReadOnlyAttribute" &&
                    attributeType.NamespaceName == "System.Runtime.CompilerServices")
                {
                    if (refKind != MetadataRefKind.None)
                    {
                        refKind = MetadataRefKind.In;
                        customAttribute.MarkProcessed();
                    }
                }
                else if (attributeType.Name == "RequiredMemberAttribute" &&
                         attributeType.NamespaceName == "System.Runtime.CompilerServices")
                {
                    isRequired = true;
                    customAttribute.MarkProcessed();
                }
            }
        }

        ImmutableInterlocked.InterlockedInitialize(ref _customAttributes, customAttributes);
        ImmutableInterlocked.InterlockedInitialize(ref _parameters, parameters);
        _refKind = refKind;
        _isRequired = isRequired;

        Interlocked.CompareExchange(ref _signature, signature, null);
    }
}