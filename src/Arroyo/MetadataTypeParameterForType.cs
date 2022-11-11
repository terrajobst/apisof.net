using System.Reflection.Metadata;

namespace Arroyo;

internal sealed class MetadataTypeParameterForType : MetadataTypeParameter
{
    private readonly MetadataNamedType _containingType;

    internal MetadataTypeParameterForType(MetadataNamedType containingType, GenericParameterHandle handle)
        : base(containingType.ContainingModule, handle)
    {
        _containingType = containingType;
    }

    public override bool IsType => true;

    public override MetadataModule ContainingModule
    {
        get { return _containingType.ContainingModule; }
    }

    public override MetadataNamedType ContainingType
    {
        get { return _containingType; }
    }

    public override MetadataMethod? ContainingMethod
    {
        get { return null; }
    }
}