namespace Arroyo;

public abstract class MetadataItem : IMetadataItem
{
    private protected MetadataItem()
    {
    }

    public abstract int Token { get; }

    public string? GetDocumentationId()
    {
        return DocumentationId.Get(this);
    }

    public override string ToString()
    {
        return this.GetDocumentationId() ?? string.Empty;
    }
}