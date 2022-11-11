using System.Collections.Immutable;
using System.Diagnostics;

namespace Arroyo.Signatures;

public sealed class MetadataNamedTypeInstance : MetadataNamedTypeReference
{
    private readonly MetadataNamedTypeReference? _containingType;
    private readonly MetadataNamedTypeReference _genericType;
    private readonly ImmutableArray<MetadataType> _typeArguments;

    internal MetadataNamedTypeInstance(MetadataNamedTypeReference genericType, ImmutableArray<MetadataType> typeArguments)
    {
        if (genericType is null)
            throw new ArgumentNullException("genericType");

        var c = genericType.ContainingType;
        var startIndex = 0;
        while (c is not null)
        {
            startIndex += c.GenericArity;
            c = c.ContainingType;
        }

        if (startIndex == 0)
        {
            _containingType = genericType.ContainingType;
        }
        else
        {
            Debug.Assert(genericType.ContainingType is not null);
            _containingType = new MetadataNamedTypeInstance(genericType.ContainingType, typeArguments);
        }

        var endIndex = startIndex + genericType.GenericArity;
        var endCount = typeArguments.Length - endIndex;

        if (endCount > 0)
            typeArguments = typeArguments.RemoveRange(endIndex, endCount);

        // It's not clear to me why we need the upper bounds check.
        // It seems that state should be impossible...
        if (startIndex > 0 && startIndex <= typeArguments.Length)
            typeArguments = typeArguments.RemoveRange(0, startIndex);

        _genericType = genericType;
        _typeArguments = typeArguments;
    }

    public override int Token
    {
        get { return default; }
    }

    public override MetadataFile ContainingFile
    {
        get { return _genericType.ContainingFile; }
    }

    public override string Name
    {
        get { return _genericType.Name; }
    }

    public override string NamespaceName
    {
        get { return _genericType.NamespaceName; }
    }

    public override int GenericArity
    {
        get { return 0; }
    }

    private protected override MetadataNamedTypeReference? ContainingTypeCore
    {
        get { return _containingType; }
    }

    public MetadataNamedTypeReference GenericType
    {
        get { return _genericType; }
    }

    public ImmutableArray<MetadataType> TypeArguments
    {
        get { return _typeArguments; }
    }
}