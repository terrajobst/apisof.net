using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Shared;

public static class Link
{
    public static string ForCatalog()
    {
        return "/catalog";
    }

    public static string ForApis()
    {
        return $"/catalog/apis";
    }

    public static string For(ApiModel api)
    {
        return ForApi(api.Guid);
    }

    public static string For(ExtensionMethodModel extensionMethod)
    {
        return ForApi(extensionMethod.Guid);
    }

    private static string ForApi(Guid guid)
    {
        return $"/catalog/{guid:N}";
    }

    public static string ForFrameworks()
    {
        return $"/catalog/frameworks";
    }

    public static string For(FrameworkModel framework)
    {
        return $"/catalog/frameworks/{framework.Name}";
    }

    public static string ForPackages()
    {
        return $"/catalog/packages";
    }

    public static string For(PackageModel package)
    {
        return $"/catalog/packages/{package.Name}/{package.Version}";
    }

    public static string ForAssemblies()
    {
        return $"/catalog/assemblies";
    }

    public static string For(AssemblyModel assembly)
    {
        return $"/catalog/assemblies/{assembly.Guid:N}";
    }
}