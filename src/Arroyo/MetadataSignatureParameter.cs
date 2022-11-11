using System.Collections.Immutable;

namespace Arroyo;

public sealed class MetadataSignatureParameter
{
    private readonly bool _isByRef;
    private readonly MetadataType _parameterType;
    private readonly ImmutableArray<MetadataCustomModifier> _customModifiers;
    private readonly ImmutableArray<MetadataCustomModifier> _refCustomModifiers;

    internal MetadataSignatureParameter(MetadataType type)
    {
        MetadataTypeProcessor.Process(ref type,
                                      out var isByRef,
                                      out var customModifiers,
                                      out var refCustomModifiers);

        _isByRef = isByRef;
        _parameterType = type;
        _customModifiers = customModifiers;
        _refCustomModifiers = refCustomModifiers;
    }

    public bool IsByRef
    {
        get { return _isByRef; }
    }

    public MetadataType ParameterType
    {
        get { return _parameterType; }
    }

    public ImmutableArray<MetadataCustomModifier> CustomModifiers
    {
        get { return _customModifiers; }
    }

    public ImmutableArray<MetadataCustomModifier> RefCustomModifiers
    {
        get { return _refCustomModifiers; }
    }

    public MetadataType ToRawType()
    {
        return MetadataTypeProcessor.Combine(_parameterType,
                                             _isByRef,
                                             _customModifiers,
                                             _refCustomModifiers);
    }
}