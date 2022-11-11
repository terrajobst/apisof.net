namespace Arroyo.Signatures;

internal readonly struct MetadataGenericContext
{
    public MetadataGenericContext(MetadataNamedType genericType)
    {
        GenericMethod = null;
        GenericType = genericType;
    }

    public MetadataGenericContext(MetadataMethod genericMethod)
    {
        GenericMethod = genericMethod;
        GenericType = genericMethod.ContainingType;
    }

    public MetadataMethod? GenericMethod { get; }

    public MetadataNamedType? GenericType { get; }
}