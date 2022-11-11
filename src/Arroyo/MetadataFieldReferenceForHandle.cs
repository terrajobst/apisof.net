using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Arroyo;

internal sealed class MetadataFieldReferenceForHandle : MetadataFieldReference
{
    private readonly MemberReferenceHandle _handle;
    private readonly MetadataType _containingType;
    private readonly MetadataType _fieldType;
    private readonly string _name;
    private readonly ImmutableArray<MetadataCustomAttribute> _customAttributes;

    internal MetadataFieldReferenceForHandle(MetadataModule containingModule, MemberReferenceHandle handle, MemberReference reference)
    {
        Debug.Assert(reference.GetKind() == MemberReferenceKind.Field);

        _handle = handle;
        _containingType = containingModule.GetTypeReference(reference.Parent, default);
        _name = containingModule.MetadataReader.GetString(reference.Name);
        _fieldType = reference.DecodeFieldSignature(containingModule.TypeProvider, default);
        _customAttributes = containingModule.GetCustomAttributes(reference.GetCustomAttributes());
    }

    public override int Token
    {
        get { return MetadataTokens.GetToken(_handle); }
    }

    private protected override MetadataType ContainingTypeCore
    {
        get { return _containingType; }
    }

    public override string Name
    {
        get { return _name; }
    }

    public override MetadataType FieldType
    {
        get { return _fieldType; }
    }

    public override MetadataCustomAttributeEnumerator GetCustomAttributes(bool includeProcessed = false)
    {
        return new MetadataCustomAttributeEnumerator(_customAttributes, includeProcessed);
    }
}