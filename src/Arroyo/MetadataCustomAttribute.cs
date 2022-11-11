using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Arroyo.Signatures;

namespace Arroyo;

public sealed class MetadataCustomAttribute
{
    private readonly MetadataModule _containingModule;
    private readonly CustomAttributeHandle _handle;

    private bool _isProcessed;
    private MetadataMethodReference? _constructor;
    private ImmutableArray<MetadataTypedValue> _fixedArguments;
    private ImmutableArray<MetadataCustomAttributeNamedArgument> _namedArguments;

    internal MetadataCustomAttribute(MetadataModule containingModule,
                                     CustomAttributeHandle handle)
    {
        _containingModule = containingModule;
        _handle = handle;
    }

    public int Token
    {
        get { return MetadataTokens.GetToken(_handle); }
    }

    public MetadataMethodReference Constructor
    {
        get
        {
            if (_constructor is null)
            {
                var constructor = ComputeConstructor();
                Interlocked.CompareExchange(ref _constructor, constructor, null);
            }

            return _constructor;
        }
    }

    public bool IsProcessed
    {
        get { return _isProcessed; }
    }

    public ImmutableArray<MetadataTypedValue> FixedArguments
    {
        get
        {
            if (_fixedArguments.IsDefault)
                InitializeArguments();

            return _fixedArguments;
        }
    }

    public ImmutableArray<MetadataCustomAttributeNamedArgument> NamedArguments
    {
        get
        {
            if (_namedArguments.IsDefault)
                InitializeArguments();

            return _namedArguments;
        }
    }

    private MetadataMethodReference ComputeConstructor()
    {
        var customAttribute = _containingModule.MetadataReader.GetCustomAttribute(_handle);
        return _containingModule.GetMethodReference(customAttribute.Constructor);
    }

    private void InitializeArguments()
    {
        var customAttribute = _containingModule.MetadataReader.GetCustomAttribute(_handle);
        var provider = new GuessingCustomAttributeTypeProvider(_containingModule.TypeProvider);
        var decoder = new MetadataCustomAttributeDecoder<MetadataType>(provider, _containingModule.MetadataReader);

        CustomAttributeValue<MetadataType> value = default;

    TryAgain:
        try
        {
            while (!decoder.TryDecodeValue(customAttribute.Constructor, customAttribute.Value, out value) &&
                   provider.TryNextPermutation())
            {
            }

            provider.Commit();
        }
        catch (Exception)
        {
            GlobalStats.IncrementCustomAttributes();

            if (provider.TryNextPermutation())
                goto TryAgain;
        }

        var fixedArguments = !value.FixedArguments.IsDefault
            ? ComputeFixedArguments(value.FixedArguments)
            : ImmutableArray<MetadataTypedValue>.Empty;

        ImmutableInterlocked.InterlockedInitialize(ref _fixedArguments, fixedArguments);

        var namedArguments = !value.NamedArguments.IsDefault
            ? ComputeNamedArguments(value.NamedArguments)
            : ImmutableArray<MetadataCustomAttributeNamedArgument>.Empty;

        ImmutableInterlocked.InterlockedInitialize(ref _namedArguments, namedArguments);
    }

    private static ImmutableArray<MetadataTypedValue> ComputeFixedArguments(ImmutableArray<CustomAttributeTypedArgument<MetadataType>> fixedArguments)
    {
        var result = ImmutableArray.CreateBuilder<MetadataTypedValue>(fixedArguments.Length);

        foreach (var fixedArgument in fixedArguments)
        {
            var typedValue = CreateTypedValue(fixedArgument.Value, fixedArgument.Type);
            result.Add(typedValue);
        }

        return result.ToImmutable();
    }

    private static ImmutableArray<MetadataCustomAttributeNamedArgument> ComputeNamedArguments(ImmutableArray<CustomAttributeNamedArgument<MetadataType>> namedArguments)
    {
        var result = ImmutableArray.CreateBuilder<MetadataCustomAttributeNamedArgument>(namedArguments.Length);

        foreach (var namedArgument in namedArguments)
        {
            var name = namedArgument.Name ?? string.Empty;
            var realValue = namedArgument.Value;
            var value = CreateTypedValue(realValue, namedArgument.Type);
            var arg = new MetadataCustomAttributeNamedArgument(name, namedArgument.Kind, value);
            result.Add(arg);
        }

        return result.ToImmutable();
    }

    private static MetadataTypedValue CreateTypedValue(object? value, MetadataType type)
    {
        return new MetadataTypedValue(type, value);
    }

    internal void MarkProcessed()
    {
        _isProcessed = true;
    }
}