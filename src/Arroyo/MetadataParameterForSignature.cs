using System.Collections.Immutable;
using System.Reflection;

namespace Arroyo;

internal sealed class MetadataParameterForSignature : MetadataParameter
{
    private readonly MetadataSignature _signature;
    private readonly int _sequenceNumber;

    public MetadataParameterForSignature(MetadataSignature signature, int sequenceNumber)
    {
        _signature = signature;
        _sequenceNumber = sequenceNumber;
    }

    public override int Token
    {
        get { return default; }
    }

    public override string Name
    {
        get { return string.Empty; }
    }

    public override int SequenceNumber
    {
        get { return _sequenceNumber; }
    }

    public override MetadataType ParameterType
    {
        get { return _signature.Parameters[_sequenceNumber].ParameterType; }
    }

    public override MetadataRefKind RefKind
    {
        get
        {
            return _signature.Parameters[_sequenceNumber].IsByRef ? MetadataRefKind.Ref : MetadataRefKind.None;
        }
    }

    public override bool IsParams
    {
        get { return false; }
    }

    public override MetadataConstant? DefaultValue
    {
        get { return null; }
    }

    public override ImmutableArray<MetadataCustomModifier> CustomModifiers
    {
        get { return _signature.Parameters[_sequenceNumber].CustomModifiers; }
    }

    public override ImmutableArray<MetadataCustomModifier> RefCustomModifiers
    {
        get { return _signature.Parameters[_sequenceNumber].RefCustomModifiers; }
    }

    public override MetadataCustomAttributeEnumerator GetCustomAttributes(bool includeProcessed = false)
    {
        return MetadataCustomAttributeEnumerator.Empty;
    }

    private protected override ParameterAttributes ParameterAttributes
    {
        get { return ParameterAttributes.None; }
    }
}