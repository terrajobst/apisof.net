using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Shared;

public static class Link
{
    public static string ForCatalog()
    {
        return "/catalog";
    }

    public static string For(ApiModel api)
    {
        ThrowIfDefault(api);

        return ForApi(api.Guid);
    }

    public static string For(ExtensionMethodModel extensionMethod)
    {
        ThrowIfDefault(extensionMethod);

        return ForApi(extensionMethod.Guid);
    }

    private static string ForApi(Guid guid)
    {
        return $"/catalog/{guid:N}";
    }
}