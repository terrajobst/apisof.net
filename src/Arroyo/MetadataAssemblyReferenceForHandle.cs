using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Arroyo;

public sealed class MetadataAssemblyReferenceForHandle : MetadataAssemblyReference
{
    private readonly MetadataModule _containingModule;
    private readonly AssemblyReferenceHandle _handle;

    private string? _name;
    private MetadataAssemblyIdentity? _identity;
    private ImmutableArray<MetadataCustomAttribute> _customAttributes;

    internal MetadataAssemblyReferenceForHandle(MetadataModule containingModule,
                                                AssemblyReferenceHandle handle)
    {
        _containingModule = containingModule;
        _handle = handle;
    }

    public override int Token
    {
        get { return MetadataTokens.GetToken(_handle); }
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
        var reference = _containingModule.MetadataReader.GetAssemblyReference(_handle);
        return _containingModule.MetadataReader.GetString(reference.Name);
    }

    private MetadataAssemblyIdentity ComputeIdentity()
    {
        var reference = _containingModule.MetadataReader.GetAssemblyReference(_handle);
        var name = Name;
        var culture = _containingModule.MetadataReader.GetString(reference.Culture);
        var hasPublicKey = reference.Flags.IsFlagSet(AssemblyFlags.PublicKey);
        var publicKeyOrToken = _containingModule.MetadataReader.GetBlobContent(reference.PublicKeyOrToken);
        var flags = reference.Flags;
        var version = reference.Version;
        // Is this correct?
        var hashAlgorithm = AssemblyHashAlgorithm.None;
        return new MetadataAssemblyIdentity(name, culture, flags, hashAlgorithm, publicKeyOrToken, hasPublicKey, version);
    }

    private ImmutableArray<MetadataCustomAttribute> ComputeCustomAttributes()
    {
        var reference = _containingModule.MetadataReader.GetAssemblyReference(_handle);
        var customAttributes = reference.GetCustomAttributes();
        return _containingModule.GetCustomAttributes(customAttributes);
    }
}