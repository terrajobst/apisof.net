#nullable enable

using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Shared;

public static class Filters
{
    public static bool Match(ApiModel item, FilterText text)
    {
        return Match(item.Name, text);
    }
    
    public static bool Match(FrameworkModel item, FilterText text)
    {
        return Match(item.Name, text);
    }

    public static bool Match(AssemblyModel item, FilterText text)
    {
        return Match(item.Name, text);
    }

    public static bool Match(PackageModel item, FilterText text)
    {
        return Match(item.Name, text);
    }

    private static bool Match(string name, FilterText text)
    {
        foreach (var entry in text.IncludedTerms)
        {
            if (!name.Contains(entry, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        foreach (var entry in text.ExcludedTerms)
        {
            if (name.Contains(entry, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}