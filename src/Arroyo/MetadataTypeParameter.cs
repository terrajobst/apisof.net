using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Arroyo.Signatures;

namespace Arroyo;

public abstract class MetadataTypeParameter : MetadataTypeParameterReference
{
    private readonly GenericParameterHandle _handle;
    private readonly int _index;
    private readonly GenericParameterAttributes _attributes;

    private string? _name;
    private ImmutableArray<MetadataGenericParameterConstraint> _constraints;
    private ImmutableArray<MetadataCustomAttribute> _customAttributes;

    private protected MetadataTypeParameter(MetadataModule containingModule, GenericParameterHandle handle)
    {
        _handle = handle;
        var genericParameter = containingModule.MetadataReader.GetGenericParameter(handle);
        _index = genericParameter.Index;
        _attributes = genericParameter.Attributes;
    }

    public abstract MetadataModule ContainingModule { get; }

    public abstract MetadataNamedType ContainingType { get; }

    public abstract MetadataMethod? ContainingMethod { get; }

    public override int Token
    {
        get { return MetadataTokens.GetToken(_handle); }
    }

    public override int Index
    {
        get { return _index; }
    }

    public string Name
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

    public VarianceKind Variance
    {
        get { return _attributes.GetVarianceKind(); }
    }

    public bool HasConstructorConstraint
    {
        get
        {
            return _attributes.IsFlagSet(GenericParameterAttributes.DefaultConstructorConstraint);
        }
    }

    public bool HasReferenceTypeConstraint
    {
        get
        {
            return _attributes.IsFlagSet(GenericParameterAttributes.ReferenceTypeConstraint);
        }
    }

    public bool HasValueTypeConstraint
    {
        get
        {
            return _attributes.IsFlagSet(GenericParameterAttributes.NotNullableValueTypeConstraint);
        }
    }

    public ImmutableArray<MetadataGenericParameterConstraint> Constraints
    {
        get
        {
            if (_constraints.IsDefault)
            {
                var constraints = ComputeConstraints();
                ImmutableInterlocked.InterlockedInitialize(ref _constraints, constraints);
            }

            return _constraints;
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

    public bool HasConstraints()
    {
        return HasConstructorConstraint ||
               HasReferenceTypeConstraint ||
               HasValueTypeConstraint ||
               Constraints.Any();
    }

    private string ComputeName()
    {
        var genericParameter = ContainingModule.MetadataReader.GetGenericParameter(_handle);
        return ContainingModule.MetadataReader.GetString(genericParameter.Name);
    }

    private ImmutableArray<MetadataGenericParameterConstraint> ComputeConstraints()
    {
        var genericParameter = ContainingModule.MetadataReader.GetGenericParameter(_handle);
        var constraints = genericParameter.GetConstraints();
        var result = ImmutableArray.CreateBuilder<MetadataGenericParameterConstraint>(constraints.Count);

        foreach (var constraintHandle in constraints)
        {
            var constraint = new MetadataGenericParameterConstraint(this, constraintHandle);
            result.Add(constraint);
        }

        return result.ToImmutable();
    }

    private ImmutableArray<MetadataCustomAttribute> ComputeCustomAttributes()
    {
        var genericParameter = ContainingModule.MetadataReader.GetGenericParameter(_handle);
        var customAttributes = genericParameter.GetCustomAttributes();
        return ContainingModule.GetCustomAttributes(customAttributes);
    }

    public override string ToString()
    {
        return Name;
    }
}