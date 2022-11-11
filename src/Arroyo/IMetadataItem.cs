namespace Arroyo;

public interface IMetadataItem
{
    int Token { get; }

    string? GetDocumentationId();
}