using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Arroyo;

public sealed class MetadataFileReference : MetadataFile
{
    private MetadataModule _containingModule;
    private AssemblyFileHandle _handle;
    private bool _containsMetadata;

    private string? _name;
    private ImmutableArray<MetadataCustomAttribute> _customAttributes;

    internal MetadataFileReference(MetadataModule containingModule, AssemblyFileHandle handle)
    {
        _containingModule = containingModule;
        var definition = containingModule.MetadataReader.GetAssemblyFile(handle);
        _containsMetadata = definition.ContainsMetadata;
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

    public bool ContainsMetadata
    {
        get { return _containsMetadata; }
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
        var definition = _containingModule.MetadataReader.GetAssemblyFile(_handle);
        return _containingModule.MetadataReader.GetString(definition.Name);
    }

    private ImmutableArray<MetadataCustomAttribute> ComputeCustomAttributes()
    {
        var definition = _containingModule.MetadataReader.GetAssemblyFile(_handle);
        var customAttributes = definition.GetCustomAttributes();
        return _containingModule.GetCustomAttributes(customAttributes);
    }
}