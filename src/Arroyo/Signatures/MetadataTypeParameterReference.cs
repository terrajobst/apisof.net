namespace Arroyo.Signatures;

public abstract class MetadataTypeParameterReference : MetadataType
{
    private protected MetadataTypeParameterReference()
    {
    }

    public override int Token
    {
        get { return default; }
    }

    public abstract bool IsType { get; }

    public abstract int Index { get; }
}