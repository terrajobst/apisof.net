using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Arroyo;

public sealed class MetadataParameterForHandle : MetadataParameter
{
    private readonly MetadataMethod _containingMethod;
    private readonly ParameterHandle _handle;
    private readonly ParameterAttributes _attributes;
    private readonly int _sequenceNumber;

    private string? _name;
    private MetadataSignatureParameter? _signatureParameter;
    private MetadataRefKind _refKind;
    private bool _isParams;
    private ImmutableArray<MetadataCustomAttribute> _customAttributes;
    private MetadataConstant? _defaultValue;

    internal MetadataParameterForHandle(MetadataMethod containingMethod,
                                        ParameterHandle handle)
    {
        _containingMethod = containingMethod;
        _handle = handle;
        var parameter = _containingMethod.ContainingType.ContainingModule.MetadataReader.GetParameter(handle);
        _attributes = parameter.Attributes;
        _sequenceNumber = parameter.SequenceNumber;
        // TODO: _parameter.GetMarshallingDescriptor()
    }

    public override int Token
    {
        get { return MetadataTokens.GetToken(_handle); }
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

    public override int SequenceNumber
    {
        get { return _sequenceNumber; }
    }

    public override MetadataType ParameterType
    {
        get
        {
            EnsureSignatureLoaded();
            return _signatureParameter.ParameterType;
        }
    }

    public override MetadataRefKind RefKind
    {
        get
        {
            EnsureSignatureLoaded();
            return _refKind;
        }
    }

    public override bool IsParams
    {
        get
        {
            EnsureSignatureLoaded();
            return _isParams;
        }
    }

    public override MetadataConstant? DefaultValue
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

    public override ImmutableArray<MetadataCustomModifier> CustomModifiers
    {
        get
        {
            EnsureSignatureLoaded();
            return _signatureParameter.CustomModifiers;
        }
    }

    public override ImmutableArray<MetadataCustomModifier> RefCustomModifiers
    {
        get
        {
            EnsureSignatureLoaded();
            return _signatureParameter.RefCustomModifiers;
        }
    }

    public override MetadataCustomAttributeEnumerator GetCustomAttributes(bool includeProcessed = false)
    {
        EnsureSignatureLoaded();
        return new MetadataCustomAttributeEnumerator(_customAttributes, includeProcessed);
    }

    private protected override ParameterAttributes ParameterAttributes
    {
        get { return _attributes; }
    }

    private string ComputeName()
    {
        var parameter = _containingMethod.ContainingType.ContainingModule.MetadataReader.GetParameter(_handle);
        return _containingMethod.ContainingType.ContainingModule.MetadataReader.GetString(parameter.Name);
    }

    private MetadataConstant ComputeDefaultValue()
    {
        var parameter = _containingMethod.ContainingType.ContainingModule.MetadataReader.GetParameter(_handle);
        var constantHandle = parameter.GetDefaultValue();
        return new MetadataConstant(_containingMethod.ContainingType.ContainingModule, constantHandle);
    }

    private ImmutableArray<MetadataCustomAttribute> ComputeCustomAttributes()
    {
        var parameter = _containingMethod.ContainingType.ContainingModule.MetadataReader.GetParameter(_handle);
        var customAttributes = parameter.GetCustomAttributes();
        return _containingMethod.ContainingType.ContainingModule.GetCustomAttributes(customAttributes);
    }

    [MemberNotNull(nameof(_signatureParameter))]
    private void EnsureSignatureLoaded()
    {
        if (_signatureParameter is not null)
            return;

        var customAttributes = ComputeCustomAttributes();

        var parameterIndex = _sequenceNumber - 1;
        var signatureParameter = _containingMethod.Signature.Parameters[parameterIndex];

        var refKind = MetadataRefKind.None;
        var isParams = false;

        if (signatureParameter.IsByRef)
        {
            if (IsIn && !IsOut)
                refKind = MetadataRefKind.In;
            else if (!IsIn && IsOut)
                refKind = MetadataRefKind.Out;
            else
                refKind = MetadataRefKind.Ref;
        }

        foreach (var customAttribute in customAttributes)
        {
            if (customAttribute.FixedArguments.Length == 0 &&
                customAttribute.NamedArguments.Length == 0 &&
                customAttribute.Constructor.ContainingType is MetadataNamedTypeReference attributeType)
            {
                if (attributeType.Name == "ParamArrayAttribute" &&
                    attributeType.NamespaceName == "System")
                {
                    isParams = true;
                    customAttribute.MarkProcessed();
                }
            }
        }

        ImmutableInterlocked.InterlockedInitialize(ref _customAttributes, customAttributes);
        _refKind = refKind;
        _isParams = isParams;

        Interlocked.CompareExchange(ref _signatureParameter, signatureParameter, null);
    }
}