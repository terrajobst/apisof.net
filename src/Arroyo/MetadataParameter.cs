using System.Collections.Immutable;
using System.Reflection;

namespace Arroyo;

public abstract class MetadataParameter : MetadataItem
{
    public bool IsIn
    {
        get { return ParameterAttributes.IsFlagSet(ParameterAttributes.In); }
    }

    public bool IsOut
    {
        get { return ParameterAttributes.IsFlagSet(ParameterAttributes.Out); }
    }

    public bool HasDefault
    {
        get { return ParameterAttributes.IsFlagSet(ParameterAttributes.HasDefault); }
    }

    public bool HasFieldMarshal
    {
        get { return ParameterAttributes.IsFlagSet(ParameterAttributes.HasFieldMarshal); }
    }

    public bool IsOptional
    {
        get { return ParameterAttributes.IsFlagSet(ParameterAttributes.Optional); }
    }

    public bool IsLocaleIdentifier
    {
        get { return ParameterAttributes.IsFlagSet(ParameterAttributes.Lcid); }
    }

    public bool IsReturnValue
    {
        get { return ParameterAttributes.IsFlagSet(ParameterAttributes.Retval); }
    }

    public abstract string Name { get; }

    public abstract int SequenceNumber { get; }

    public abstract MetadataType ParameterType { get; }

    public abstract MetadataRefKind RefKind { get; }

    public abstract bool IsParams { get; }

    public abstract MetadataConstant? DefaultValue { get; }

    public abstract ImmutableArray<MetadataCustomModifier> CustomModifiers { get; }

    public abstract ImmutableArray<MetadataCustomModifier> RefCustomModifiers { get; }

    public abstract MetadataCustomAttributeEnumerator GetCustomAttributes(bool includeProcessed = false);

    private abstract protected ParameterAttributes ParameterAttributes { get; }
}