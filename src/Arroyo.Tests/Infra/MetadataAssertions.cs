using System.Diagnostics;
using FluentAssertions.Primitives;

namespace Arroyo.Tests.Infra;

[DebuggerNonUserCode]
internal sealed class MetadataAssertions<T> : ObjectAssertions<T, MetadataAssertions<T>>
    where T: class?, IMetadataItem?
{
    public MetadataAssertions(T instance)
        : base(instance)
    {
    }
}