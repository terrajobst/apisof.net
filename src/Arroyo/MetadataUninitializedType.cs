namespace Arroyo;

internal sealed class MetadataUninitializedType : MetadataType
{
    public static MetadataUninitializedType Instance { get; } = new();

    private MetadataUninitializedType()
    {
    }

    public override int Token
    {
        get { return default; }
    }
}