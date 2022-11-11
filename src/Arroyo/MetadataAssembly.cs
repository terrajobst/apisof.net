using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Arroyo;

public sealed class MetadataAssembly : MetadataAssemblyReference, IDisposable
{
    private readonly MetadataModule _mainModule;

    private string? _name;
    private MetadataAssemblyIdentity? _identity;
    private ImmutableArray<MetadataCustomAttribute> _customAttributes;

    internal MetadataAssembly(PEReader peReader, MetadataReader metadataReader, string location)
    {
        _mainModule = new MetadataModule(this, peReader, metadataReader, location);

        // TODO: _definition.GetDeclarativeSecurityAttributes()
    }

    public override int Token
    {
        get { return default; }
    }

    public void Dispose()
    {
        _mainModule.Dispose();
    }

    public static new MetadataAssembly? Open(string path)
    {
        return MetadataFile.Open(path) as MetadataAssembly;
    }

    public static new MetadataAssembly? Open(Stream stream)
    {
        return MetadataFile.Open(stream) as MetadataAssembly;
    }

    public static new MetadataAssembly? Open(Stream stream, string location)
    {
        return MetadataFile.Open(stream, location) as MetadataAssembly;
    }

    public override string Name
    {
        get
        {
            if (_name is null)
            {
                var name = ComputeName();
                Interlocked.CompareExchange(ref _name, name, null);
            }

            return _name;
        }
    }

    public override MetadataAssemblyIdentity Identity
    {
        get
        {
            if (_identity is null)
            {
                var identity = ComputeIdentity();
                Interlocked.CompareExchange(ref _identity, identity, null);
            }

            return _identity;
        }
    }

    public MetadataModule MainModule
    {
        get { return _mainModule; }
    }

    public override MetadataCustomAttributeEnumerator GetCustomAttributes(bool includeProcessed = false)
    {
        if (_customAttributes.IsDefault)
        {
            var customAttributes = ComputeCustomAttributes();
            ImmutableInterlocked.InterlockedInitialize(ref _customAttributes, customAttributes);
        }

        return new MetadataCustomAttributeEnumerator(_customAttributes, includeProcessed);
    }

    private string ComputeName()
    {
        var definition = _mainModule.MetadataReader.GetAssemblyDefinition();
        return _mainModule.MetadataReader.GetString(definition.Name);
    }

    private MetadataAssemblyIdentity ComputeIdentity()
    {
        var definition = _mainModule.MetadataReader.GetAssemblyDefinition();
        var name = Name;
        var culture = _mainModule.MetadataReader.GetString(definition.Culture);
        var flags = definition.Flags;
        var hashAlgorithm = definition.HashAlgorithm;
        var publicKey = _mainModule.MetadataReader.GetBlobContent(definition.PublicKey);
        var version = definition.Version;
        return new MetadataAssemblyIdentity(name, culture, flags, hashAlgorithm, publicKey, true, version);
    }

    private ImmutableArray<MetadataCustomAttribute> ComputeCustomAttributes()
    {
        var definition = _mainModule.MetadataReader.GetAssemblyDefinition();
        var customAttributes = definition.GetCustomAttributes();
        return _mainModule.GetCustomAttributes(customAttributes);
    }

    public string? GetTargetFrameworkMoniker()
    {
        foreach (var customAttribute in GetCustomAttributes())
        {
            if (customAttribute.Constructor.ContainingType is MetadataNamedTypeReference named &&
                named.NamespaceName == "System.Runtime.Versioning" &&
                named.Name == "TargetFrameworkAttribute" &&
                named.GenericArity == 0 &&
                customAttribute.FixedArguments.Length == 1 &&
                customAttribute.FixedArguments[0].Value is string tfm)
            {
                return tfm;
            }
        }

        return null;
    }
}