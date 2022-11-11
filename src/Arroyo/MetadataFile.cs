using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Arroyo;

public abstract class MetadataFile : MetadataItem
{
    private protected MetadataFile()
    {
    }

    // TODO: This should be disposable, given that the caller has to dispose the assembly or module.
    public static MetadataFile? Open(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return Open(File.OpenRead(path));
    }

    public static MetadataFile? Open(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var location = GetLocation(stream);
        return Open(stream, location);
    }

    public static MetadataFile? Open(Stream stream, string location)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(location);

        PEReader? peReader = null;

        try
        {
            peReader = new PEReader(stream);
            if (!peReader.HasMetadata)
                return null;

            var metadataReader = peReader.GetMetadataReader();

            if (metadataReader.IsAssembly)
                return new MetadataAssembly(peReader, metadataReader, location);
            else
                return new MetadataModule(null, peReader, metadataReader, location);
        }
        catch (Exception)
        {
            GlobalStats.IncrementFile();
            peReader?.Dispose();
            return null;
        }
    }

    private static string GetLocation(Stream stream)
    {
        var fileStream = stream as FileStream;
        return fileStream is not null
            ? fileStream.Name
            : string.Empty;
    }

    public abstract string Name { get; }

    public abstract MetadataCustomAttributeEnumerator GetCustomAttributes(bool includeProcessed = false);
}