using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Arroyo;

public sealed class MetadataConstant
{
    private readonly MetadataModule _containingModule;
    private readonly ConstantHandle _handle;
    private readonly ConstantTypeCode _typeCode;

    private object? _value = MetadataExtensions.UninitializedValue;

    internal MetadataConstant(MetadataModule containingModule, ConstantHandle handle)
    {
        _containingModule = containingModule;
        _handle = handle;
        var constant = containingModule.MetadataReader.GetConstant(handle);
        _typeCode = constant.TypeCode;
    }

    public int Token
    {
        get { return MetadataTokens.GetToken(_handle); }
    }

    public ConstantTypeCode TypeCode
    {
        get { return _typeCode; }
    }

    public object? Value
    {
        get
        {
            if (_value == MetadataExtensions.UninitializedValue)
            {
                var value = ComputeValue();
                Interlocked.CompareExchange(ref _value, value, MetadataExtensions.UninitializedValue);
            }

            return _value;
        }
    }

    private object? ComputeValue()
    {
        var constant = _containingModule.MetadataReader.GetConstant(_handle);
        var blobReader = _containingModule.MetadataReader.GetBlobReader(constant.Value);
        return blobReader.ReadConstant(constant.TypeCode);
    }
}