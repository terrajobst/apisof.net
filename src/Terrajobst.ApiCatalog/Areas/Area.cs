using System.Collections.Immutable;

namespace Terrajobst.ApiCatalog;

public sealed partial class Area
{
    public Area(string name)
    {
        ThrowIfNullOrEmpty(name);

        Name = name;
    }

    public string Name { get; }

    public required ImmutableArray<AreaMatcher> Include { get; init; }
}
