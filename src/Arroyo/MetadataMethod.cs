using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Arroyo.Signatures;

namespace Arroyo;

public sealed class MetadataMethod : MetadataMethodReference, IMetadataTypeMember
{
    private readonly MetadataNamedType _containingType;
    private readonly MethodDefinitionHandle _handle;
    private readonly MethodAttributes _attributes;
    private MetadataMember? _associatedMember;

    private string? _name;
    private MethodKind _kind = MetadataExtensions.UninitializedMethodKind;
    private ImmutableArray<MetadataTypeParameter> _genericParameters;
    private MetadataSignature? _signature;
    private MetadataRefKind _refKind;
    private bool _isReadOnly;
    private ImmutableArray<MetadataParameter> _parameters;
    private ImmutableArray<MetadataCustomAttribute> _customAttributes;

    internal MetadataMethod(MetadataNamedType containingType, MethodDefinitionHandle handle)
    {
        _containingType = containingType;
        _handle = handle;
        var definition = containingType.ContainingModule.MetadataReader.GetMethodDefinition(handle);
        _attributes = definition.Attributes;

        // TODO: _definition.GetDeclarativeSecurityAttributes()
        // TODO: _definition.GetImport()
        // TODO: _definition.ImplAttributes
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

    public MetadataAccessibility Accessibility
    {
        get { return _attributes.GetAccessibility(); }
    }

    public bool IsPrivateScope
    {
        get { return _attributes.IsFlagSet(MethodAttributes.PrivateScope); }
    }

    public bool IsStatic
    {
        get { return _attributes.IsFlagSet(MethodAttributes.Static); }
    }

    public bool IsSealed
    {
        get
        {
            return _attributes.IsFlagSet(MethodAttributes.Final) &&
                   !IsAbstract && IsOverride;
        }
    }

    public bool IsVirtual
    {
        get
        {
            return _attributes.IsFlagSet(MethodAttributes.Virtual) &&
                   !_attributes.IsFlagSet(MethodAttributes.Final) &&
                   !_attributes.IsFlagSet(MethodAttributes.Abstract) &&
                   (_containingType.IsInterface
                       ? (IsStatic || IsNewSlot)
                       : !IsOverride);
        }
    }

    public bool IsOverride
    {
        get
        {
            return _attributes.IsFlagSet(MethodAttributes.Virtual) &&
                     !IsNewSlot;
        }
    }

    public bool IsHideBySig
    {
        get { return _attributes.IsFlagSet(MethodAttributes.HideBySig); }
    }

    public bool CheckAccessOnOverride
    {
        get { return _attributes.IsFlagSet(MethodAttributes.CheckAccessOnOverride); }
    }

    public bool IsAbstract
    {
        get { return _attributes.IsFlagSet(MethodAttributes.Abstract); }
    }

    public bool IsSpecialName
    {
        get { return _attributes.IsFlagSet(MethodAttributes.SpecialName); }
    }

    public bool IsExtern
    {
        get { return _attributes.IsFlagSet(MethodAttributes.PinvokeImpl); }
    }

    public bool IsUnmanagedExport
    {
        get { return _attributes.IsFlagSet(MethodAttributes.UnmanagedExport); }
    }

    public bool IsRuntimeSpecialName
    {
        get { return _attributes.IsFlagSet(MethodAttributes.RTSpecialName); }
    }

    public bool HasSecurity
    {
        get { return _attributes.IsFlagSet(MethodAttributes.HasSecurity); }
    }

    public bool RequireSecurityObject
    {
        get { return _attributes.IsFlagSet(MethodAttributes.RequireSecObject); }
    }

    public bool IsNewSlot
    {
        get { return (_attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.NewSlot; }
    }

    public bool IsVararg
    {
        get
        {
            EnsureSignatureLoaded();
            return _signature.CallingConvention == SignatureCallingConvention.VarArgs;
        }
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

    public MethodKind Kind
    {
        get
        {
            if (_kind == MetadataExtensions.UninitializedMethodKind)
                _kind = ComputeKind();

            return _kind;
        }
    }

    public ImmutableArray<MetadataTypeParameter> GenericParameters
    {
        get
        {
            if (_genericParameters.IsDefault)
            {
                var genericParameters = ComputeGenericParameters();
                ImmutableInterlocked.InterlockedInitialize(ref _genericParameters, genericParameters);
            }

            return _genericParameters;
        }
    }

    public override int GenericArity
    {
        get { return GenericParameters.Length; }
    }

    public ImmutableArray<MetadataParameter> Parameters
    {
        get
        {
            EnsureSignatureLoaded();
            return _parameters;
        }
    }

    public override MetadataSignature Signature
    {
        get
        {
            EnsureSignatureLoaded();
            return _signature;
        }
    }

    public MetadataType ReturnType
    {
        get
        {
            EnsureSignatureLoaded();
            return _signature.ReturnType;
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

    public bool IsReadOnly
    {
        get
        {
            EnsureSignatureLoaded();
            return _isReadOnly;
        }
    }

    public int RelativeVirtualAddress
    {
        get
        {
            var definition = _containingType.ContainingModule.MetadataReader.GetMethodDefinition(_handle);
            return definition.RelativeVirtualAddress;
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

    public override MetadataCustomAttributeEnumerator GetCustomAttributes(bool includeProcessed = false)
    {
        EnsureSignatureLoaded();
        return new MetadataCustomAttributeEnumerator(_customAttributes, includeProcessed);
    }

    public MetadataOperationEnumerator GetOperations()
    {
        return new MetadataOperationEnumerator(this);
    }

    public MetadataMember? AssociatedMember
    {
        get { return _associatedMember; }
    }

    private string ComputeName()
    {
        var definition = _containingType.ContainingModule.MetadataReader.GetMethodDefinition(_handle);
        return _containingType.ContainingModule.MetadataReader.GetString(definition.Name);
    }

    private MethodKind ComputeKind()
    {
        if (IsSpecialName)
        {
            if (IsRuntimeSpecialName && !IsVirtual && GenericParameters.Length == 0)
            {
                // TODO: We should also check that we return void

                if (!IsStatic && string.Equals(Name, SpecialNames.ConstructorName, StringComparison.Ordinal))
                    return MethodKind.Constructor;

                if (IsStatic && string.Equals(Name, SpecialNames.ClassConstructorName, StringComparison.Ordinal))
                    return MethodKind.ClassConstructor;
            }

            if (!IsRuntimeSpecialName && IsStatic)
            {
                // TODO: It seems that C++/CLI typically doesn't make operators static but instance methods. Do we care?

                switch (Name)
                {
                    case SpecialNames.ImplicitConversionName:
                        return MethodKind.ImplicitConversion;
                    case SpecialNames.ExplicitConversionName:
                        return MethodKind.ExplicitConversion;
                    case SpecialNames.AdditionOperatorName:
                        return MethodKind.AdditionOperator;
                    case SpecialNames.BitwiseAndOperatorName:
                        return MethodKind.BitwiseAndOperator;
                    case SpecialNames.BitwiseOrOperatorName:
                        return MethodKind.BitwiseOrOperator;
                    case SpecialNames.DecrementOperatorName:
                        return MethodKind.DecrementOperator;
                    case SpecialNames.DivisionOperatorName:
                        return MethodKind.DivisionOperator;
                    case SpecialNames.EqualityOperatorName:
                        return MethodKind.EqualityOperator;
                    case SpecialNames.ExclusiveOrOperatorName:
                        return MethodKind.ExclusiveOrOperator;
                    case SpecialNames.FalseOperatorName:
                        return MethodKind.FalseOperator;
                    case SpecialNames.GreaterThanOperatorName:
                        return MethodKind.GreaterThanOperator;
                    case SpecialNames.GreaterThanOrEqualOperatorName:
                        return MethodKind.GreaterThanOrEqualOperator;
                    case SpecialNames.IncrementOperatorName:
                        return MethodKind.IncrementOperator;
                    case SpecialNames.InequalityOperatorName:
                        return MethodKind.InequalityOperator;
                    case SpecialNames.LeftShiftOperatorName:
                        return MethodKind.LeftShiftOperator;
                    case SpecialNames.UnsignedLeftShiftOperatorName:
                        return MethodKind.UnsignedLeftShiftOperator;
                    case SpecialNames.LessThanOperatorName:
                        return MethodKind.LessThanOperator;
                    case SpecialNames.LessThanOrEqualOperatorName:
                        return MethodKind.LessThanOrEqualOperator;
                    case SpecialNames.LogicalNotOperatorName:
                        return MethodKind.LogicalNotOperator;
                    case SpecialNames.LogicalOrOperatorName:
                        return MethodKind.LogicalOrOperator;
                    case SpecialNames.LogicalAndOperatorName:
                        return MethodKind.LogicalAndOperator;
                    case SpecialNames.ModulusOperatorName:
                        return MethodKind.ModulusOperator;
                    case SpecialNames.MultiplyOperatorName:
                        return MethodKind.MultiplyOperator;
                    case SpecialNames.OnesComplementOperatorName:
                        return MethodKind.OnesComplementOperator;
                    case SpecialNames.RightShiftOperatorName:
                        return MethodKind.RightShiftOperator;
                    case SpecialNames.UnsignedRightShiftOperatorName:
                        return MethodKind.UnsignedRightShiftOperator;
                    case SpecialNames.SubtractionOperatorName:
                        return MethodKind.SubtractionOperator;
                    case SpecialNames.TrueOperatorName:
                        return MethodKind.TrueOperator;
                    case SpecialNames.UnaryNegationOperatorName:
                        return MethodKind.UnaryNegationOperator;
                    case SpecialNames.UnaryPlusOperatorName:
                        return MethodKind.UnaryPlusOperator;
                    case SpecialNames.ConcatenateOperatorName:
                        return MethodKind.ConcatenateOperator;
                    case SpecialNames.ExponentOperatorName:
                        return MethodKind.ExponentOperator;
                    case SpecialNames.IntegerDivisionOperatorName:
                        return MethodKind.IntegerDivisionOperator;
                    case SpecialNames.LikeOperatorName:
                        return MethodKind.LikeOperator;
                }
            }

            if (_associatedMember is not null)
            {
                if (_associatedMember is MetadataProperty property)
                {
                    if (this == property.Getter)
                        return MethodKind.PropertyGetter;
                    else if (this == property.Setter)
                        return MethodKind.PropertySetter;
                }
                else if (_associatedMember is MetadataEvent @event)
                {
                    if (this == @event.Adder)
                        return MethodKind.EventAdder;
                    else if (this == @event.Remover)
                        return MethodKind.EventRemover;
                    else if (this == @event.Raiser)
                        return MethodKind.EventRaiser;
                }
            }
        }

        if (!IsStatic && _attributes.IsFlagSet(MethodAttributes.Virtual) && !IsNewSlot && ContainingType.IsClass &&
            string.Equals(Name, SpecialNames.FinalizeName) && Parameters.Length == 0)
        {
            return MethodKind.Finalizer;
        }

        return MethodKind.Ordinary;
    }

    private ImmutableArray<MetadataTypeParameter> ComputeGenericParameters()
    {
        var definition = _containingType.ContainingModule.MetadataReader.GetMethodDefinition(_handle);
        var genericParameters = definition.GetGenericParameters();
        var result = ImmutableArray.CreateBuilder<MetadataTypeParameter>(genericParameters.Count);

        foreach (var genericParameterHandle in genericParameters)
        {
            var metadataGenericParameter = new MetadataTypeParameterForMethod(this, genericParameterHandle);
            result.Add(metadataGenericParameter);
        }

        return result.ToImmutable();
    }

    private ImmutableArray<MetadataParameter> ComputeParameters(MetadataSignature signature)
    {
        // NOTE: It seems some assemblies, like the ones produced by F#
        //       don't always have parameters for properties defined.
        //
        // IOW, the signature contains parameter types but the parameter
        // collection is empty.
        //
        // We address this problem by having synthesized parameters that
        // are constructed from the parameter type alone.

        var definition = _containingType.ContainingModule.MetadataReader.GetMethodDefinition(_handle);
        var parameters = definition.GetParameters();
        var parameterCount = Math.Max(parameters.Count, signature.Parameters.Length);

        var result = ImmutableArray.CreateBuilder<MetadataParameter>(parameterCount);

        if (parameters.Count == 0 && parameterCount > 0)
        {
            for (var i = 0; i < signature.Parameters.Length; i++)
            {
                var metadataParameter = new MetadataParameterForSignature(signature, i);
                result.Add(metadataParameter);
            }
        }
        else
        {
            foreach (var parameterHandle in parameters)
            {
                var metadataParameter = new MetadataParameterForHandle(this, parameterHandle);

                // TODO: Handle return parameter
                if (metadataParameter.SequenceNumber < 1)
                    continue;

                result.Add(metadataParameter);
            }
        }

        return result.ToImmutable();
    }

    private MetadataSignature ComputeSignature()
    {
        var definition = _containingType.ContainingModule.MetadataReader.GetMethodDefinition(_handle);
        var genericContext = new MetadataGenericContext(this);
        return ContainingType.ContainingModule.GetMethodSignature(definition.Signature, genericContext);
    }

    private ImmutableArray<MetadataCustomAttribute> ComputeCustomAttributes()
    {
        var definition = _containingType.ContainingModule.MetadataReader.GetMethodDefinition(_handle);
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
        var isReadOnly = false;

        foreach (var customAttribute in customAttributes)
        {
            if (customAttribute.FixedArguments.Length == 0 &&
                customAttribute.NamedArguments.Length == 0 &&
                customAttribute.Constructor.ContainingType is MetadataNamedTypeReference attributeType)
            {
                if (attributeType.Name == "IsReadOnlyAttribute" &&
                    attributeType.NamespaceName == "System.Runtime.CompilerServices")
                {
                    if (_containingType.Kind == TypeKind.Struct)
                    {
                        isReadOnly = true;
                        customAttribute.MarkProcessed();
                    }
                }
            }
        }

        foreach (var customModifier in signature.RefCustomModifiers)
        {
            if (customModifier.Type.Name == "InAttribute" &&
                customModifier.Type.NamespaceName == "System.Runtime.InteropServices")
            {
                refKind = MetadataRefKind.In;
                break;
            }
        }

        ImmutableInterlocked.InterlockedInitialize(ref _customAttributes, customAttributes);
        ImmutableInterlocked.InterlockedInitialize(ref _parameters, parameters);
        _refKind = refKind;
        _isReadOnly = isReadOnly;

        Interlocked.CompareExchange(ref _signature, signature, null);
    }

    internal void SetAssociatedMember(MetadataMember member)
    {
        _associatedMember = member;
    }
}