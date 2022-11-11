using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Arroyo;

public sealed class MetadataSignature
{
    private readonly SignatureHeader _header;
    private readonly int _requiredParameterCount;
    private readonly int _genericParameterCount;
    private readonly bool _returnsByRef;
    private readonly MetadataType _returnType;
    private readonly ImmutableArray<MetadataCustomModifier> _returnTypeCustomModifiers;
    private readonly ImmutableArray<MetadataCustomModifier> _refCustomModifiers;
    private readonly ImmutableArray<MetadataSignatureParameter> _parameters;

    internal MetadataSignature(MethodSignature<MetadataType> signature)
    {
        _header = signature.Header;
        _requiredParameterCount = signature.RequiredParameterCount;
        _genericParameterCount = signature.GenericParameterCount;

        var returnType = signature.ReturnType;
        MetadataTypeProcessor.Process(ref returnType,
                                      out var isByRef,
                                      out var customModifiers,
                                      out var refCustomModifiers);

        _returnType = returnType;
        _returnsByRef = isByRef;
        _returnTypeCustomModifiers = customModifiers;
        _refCustomModifiers = refCustomModifiers;

        var parameterBuilder = ImmutableArray.CreateBuilder<MetadataSignatureParameter>(signature.ParameterTypes.Length);

        foreach (var parameterType in signature.ParameterTypes)
        {
            var parameter = new MetadataSignatureParameter(parameterType);
            parameterBuilder.Add(parameter);
        }

        _parameters = parameterBuilder.MoveToImmutable();
    }

    public SignatureCallingConvention CallingConvention
    {
        get { return _header.CallingConvention; }
    }

    public bool HasExplicitThis
    {
        get { return _header.HasExplicitThis; }
    }

    public bool IsInstance
    {
        get { return _header.IsInstance; }
    }

    public bool IsGeneric
    {
        get { return _header.IsGeneric; }
    }

    public int RequiredParameterCount
    {
        get { return _requiredParameterCount; }
    }

    public int GenericParameterCount
    {
        get { return _genericParameterCount; }
    }

    public bool ReturnsByRef
    {
        get { return _returnsByRef; }
    }

    public MetadataType ReturnType
    {
        get { return _returnType; }
    }

    public ImmutableArray<MetadataCustomModifier> ReturnTypeCustomModifiers
    {
        get { return _returnTypeCustomModifiers; }
    }

    public ImmutableArray<MetadataCustomModifier> RefCustomModifiers
    {
        get { return _refCustomModifiers; }
    }

    public ImmutableArray<MetadataSignatureParameter> Parameters
    {
        get { return _parameters; }
    }
}