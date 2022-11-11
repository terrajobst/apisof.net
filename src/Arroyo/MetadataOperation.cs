using System.Reflection.Metadata;

namespace Arroyo;

public readonly struct MetadataOperation
{
    public MetadataOperation(ILOpCode opCode, object? argument)
    {
        OpCode = opCode;
        Argument = argument;
    }

    public ILOpCode OpCode { get; }

    public object? Argument { get; }
}