using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Arroyo;

// TODO: Reconcile with MetadataConstant
public readonly struct MetadataTypedValue
{
    private readonly MetadataType _type;
    private readonly object? _value;

    public MetadataTypedValue(MetadataType type, object? value)
    {
        _type = type;

        if (value is ImmutableArray<CustomAttributeTypedArgument<MetadataType>> values)
        {
            var builder = ImmutableArray.CreateBuilder<MetadataTypedValue>(values.Length);
            foreach (var x in values)
            {
                var convertedValue = new MetadataTypedValue(x.Type, x.Value);
                builder.Add(convertedValue);
            }
            _value = builder.MoveToImmutable();
        }
        else
        {
            _value = value;
        }
    }

    public bool IsNull
    {
        get { return _value is null; }
    }

    public bool IsArray
    {
        get { return _value is ImmutableArray<MetadataTypedValue>; }
    }

    public object? Value
    {
        get { return IsArray ? null : _value; }
    }

    public ImmutableArray<MetadataTypedValue> Values
    {
        get
        {
            return IsArray
                ? (ImmutableArray<MetadataTypedValue>)_value!
                : ImmutableArray<MetadataTypedValue>.Empty;
        }
    }

    public MetadataType ValueType
    {
        get { return _type; }
    }
}