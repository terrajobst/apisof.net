namespace Arroyo.Signatures;

public sealed class MetadataFunctionPointerType : MetadataType
{
    private readonly MetadataSignature _signature;

    internal MetadataFunctionPointerType(MetadataSignature signature)
    {
        _signature = signature;
    }

    public override int Token
    {
        get { return default; }
    }

    public MetadataSignature Signature
    {
        get { return _signature; }
    }
}