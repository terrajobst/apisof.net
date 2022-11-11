using System.Reflection.Metadata;

namespace Arroyo;

internal sealed class MetadataTypeParameterForMethod : MetadataTypeParameter
{
    private readonly MetadataMethod _containingMethod;

    internal MetadataTypeParameterForMethod(MetadataMethod containingMethod,
                                            GenericParameterHandle handle)
        : base(containingMethod.ContainingType.ContainingModule, handle)
    {
        _containingMethod = containingMethod;
    }

    public override bool IsType => false;

    public override MetadataModule ContainingModule
    {
        get { return _containingMethod.ContainingType.ContainingModule; }
    }

    public override MetadataNamedType ContainingType
    {
        get { return _containingMethod.ContainingType; }
    }

    public override MetadataMethod ContainingMethod
    {
        get { return _containingMethod; }
    }
}