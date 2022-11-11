using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Arroyo.Signatures;

namespace Arroyo;

public sealed class MetadataInterfaceImplementation
{
    private readonly MetadataNamedType _containingType;
    private readonly InterfaceImplementationHandle _handle;

    private MetadataType? _interface;
    private ImmutableArray<MetadataCustomAttribute> _customAttributes;

    internal MetadataInterfaceImplementation(MetadataNamedType containingType,
                                             InterfaceImplementationHandle handle)
    {
        _containingType = containingType;
        _handle = handle;
    }

    public int Token
    {
        get { return MetadataTokens.GetToken(_handle); }
    }

    public MetadataNamedType ContainingType
    {
        get { return _containingType; }
    }

    public MetadataType Interface
    {
        get
        {
            if (_interface is null)
            {
                var iface = ComputeInterface();
                Interlocked.CompareExchange(ref _interface, iface, null);
            }

            return _interface;
        }
    }

    public ImmutableArray<MetadataCustomAttribute> CustomAttributes
    {
        get
        {
            if (_customAttributes.IsDefault)
            {
                var customAttributes = ComputeCustomAttributes();
                ImmutableInterlocked.InterlockedInitialize(ref _customAttributes, customAttributes);
            }

            return _customAttributes;
        }
    }

    private MetadataType ComputeInterface()
    {
        var interfaceImplementation = _containingType.ContainingModule.MetadataReader.GetInterfaceImplementation(_handle);
        var genericContext = new MetadataGenericContext(ContainingType);
        return ContainingType.ContainingModule.GetTypeReference(interfaceImplementation.Interface, genericContext)!;
    }

    private ImmutableArray<MetadataCustomAttribute> ComputeCustomAttributes()
    {
        var interfaceImplementation = _containingType.ContainingModule.MetadataReader.GetInterfaceImplementation(_handle);
        var customAttributes = interfaceImplementation.GetCustomAttributes();
        return _containingType.ContainingModule.GetCustomAttributes(customAttributes);
    }
}