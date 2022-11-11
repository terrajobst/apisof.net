namespace Arroyo.Signatures;

internal sealed class MetadataTypeParameterReferenceForIndex : MetadataTypeParameterReference
{
    private readonly bool _isType;
    private readonly int _index;

    internal MetadataTypeParameterReferenceForIndex(bool isType, int index)
    {
        _isType = isType;
        _index = index;
    }

    public override bool IsType
    {
        get { return _isType; }
    }

    public override int Index
    {
        get { return _index; }
    }
}