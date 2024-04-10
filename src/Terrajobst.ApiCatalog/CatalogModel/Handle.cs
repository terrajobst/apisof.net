namespace Terrajobst.ApiCatalog;

internal static class OffsetTag
{
    public sealed class String;
    public sealed class Blob;
    public sealed class Framework;
    public sealed class Package;
    public sealed class Assembly;
    public sealed class UsageSource;
    public sealed class Api;
    public sealed class ApiDeclaration;
    public sealed class ApiUsage;
}

internal readonly struct Handle<T>(int value) : IEquatable<Handle<T>>
{
    public static Handle<T> Nil { get; } = new(-1);

    public bool IsNil => this == Nil;

    public int Value { get; } = value;

    public bool Equals(Handle<T> other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Handle<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value;
    }

    public static bool operator ==(Handle<T> left, Handle<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Handle<T> left, Handle<T> right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"{Value} ({typeof(T).Name})";
    }
}
