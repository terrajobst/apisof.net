using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Arroyo.Signatures;

namespace Arroyo;

public sealed class MetadataGenericParameterConstraint
{
    private readonly MetadataTypeParameter _containingParameter;
    private readonly GenericParameterConstraintHandle _handle;

    private MetadataType? _constraintType;
    private ImmutableArray<MetadataCustomAttribute> _customAttributes;

    internal MetadataGenericParameterConstraint(MetadataTypeParameter containingParameter,
                                                GenericParameterConstraintHandle handle)
    {
        _containingParameter = containingParameter;
        _handle = handle;
    }

    public int Token
    {
        get { return MetadataTokens.GetToken(_handle); }
    }

    public MetadataType ContraintType
    {
        get
        {
            if (_constraintType is null)
            {
                var metadataType = ComputeConstraintType();
                Interlocked.CompareExchange(ref _constraintType, metadataType, null);
            }

            return _constraintType;
        }
    }

    public ImmutableArray<MetadataCustomAttribute> CustomAttributes
    {
        get
        {
            if (_customAttributes.IsDefault)
            {
                var customAttributes = ComputeCustomAttributes();
                ImmutableInterlocked.InterlockedInitialize(ref _customAttributes, customAttributes);
            }

            return _customAttributes;
        }
    }

    private MetadataType ComputeConstraintType()
    {
        var constraint = _containingParameter.ContainingModule.MetadataReader.GetGenericParameterConstraint(_handle);
        var genericContext = _containingParameter.ContainingMethod is not null
            ? new MetadataGenericContext(_containingParameter.ContainingMethod)
            : new MetadataGenericContext(_containingParameter.ContainingType!);

        return _containingParameter.ContainingModule.GetTypeReference(constraint.Type, genericContext)!;
    }

    private ImmutableArray<MetadataCustomAttribute> ComputeCustomAttributes()
    {
        var constraint = _containingParameter.ContainingModule.MetadataReader.GetGenericParameterConstraint(_handle);
        var customAttributes = constraint.GetCustomAttributes();
        return _containingParameter.ContainingModule.GetCustomAttributes(customAttributes);
    }
}