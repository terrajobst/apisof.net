#nullable enable
using ApisOfDotNet.Services;
using ApisOfDotNet.Shared;
using Microsoft.AspNetCore.Components;
using NuGet.Frameworks;
using NuGet.Versioning;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Pages;

public partial class PackageItem : IApiOutlineOwner
{
    [Inject]
    public required CatalogService CatalogService { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Parameter]
    public string? PackageName { get; set; }

    [Parameter]
    public string? PackageVersion { get; set; }

    [Parameter]
    public string? ApiGuidText { get; set; }

    [SupplyParameterFromQuery(Name = "p")]
    public string? CurrentPage { get; set; }

    public PackageModel? Package { get; set; }

    public ApiOutlineData? OutlineData { get; set; }

    private ApiFilter _apiFilter = ApiFilter.Everything;

    protected override void OnParametersSet()
    {
        Package = (PackageName, PackageVersion) switch {
            (not (null or ""), null or "") =>
                CatalogService.Catalog.Packages
                    .Where(a => string.Equals(a.Name, PackageName, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(a => NuGetVersion.Parse(a.Version))
                    .Cast<PackageModel?>()
                    .FirstOrDefault(),
            (not (null or ""), not (null or "")) =>
                CatalogService.Catalog.Packages
                    .Where(a => string.Equals(a.Name, PackageName, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(a.Version, PackageVersion, StringComparison.OrdinalIgnoreCase))
                    .Cast<PackageModel?>()
                    .FirstOrDefault(),
            _ => null
        };

        if (Package is not null)
        {
            OutlineData = ApiGuidText is null
                ? ApiOutlineData.CreateRoot(Package.Value)
                : ApiOutlineData.CreateNode(CatalogService.Catalog, ApiGuidText);
            
            _apiFilter = ApiFilter.ForPackage(Package.Value);
        }
        
        if (string.IsNullOrEmpty(CurrentPage))
            CurrentPage = "apis";
    }

    public IEnumerable<(FrameworkModel Framework, IReadOnlyList<AssemblyModel> Assemblies)> Assemblies
    {
        get
        {
            if (Package is null)
                return [];

            return Package.Value.Assemblies
                .GroupBy(t => t.Framework)
                .Select(g => (Framework: g.Key, Assemblies: (IReadOnlyList<AssemblyModel>)g
                    .Select(t => t.Assembly)
                    .OrderBy(a => a.Name)
                    .ThenBy(a => System.Version.Parse(a.Version))
                    .ToArray()))
                .Select(t => (t.Framework, NuGetFramework: NuGetFramework.Parse(t.Framework.Name), t.Assemblies))
                .OrderBy(t => t.NuGetFramework.Framework)
                .ThenByDescending(t => t.NuGetFramework.Version)
                .ThenBy(t => t.NuGetFramework.Platform)
                .ThenByDescending(t => t.NuGetFramework.PlatformVersion)
                .Select(t => (t.Framework, t.Assemblies));
        }
    }

    public IEnumerable<PackageModel> Versions
    {
        get
        {
            if (Package is null)
                return [];

            return CatalogService.Catalog.Packages
                .Where(p => string.Equals(Package.Value.Name, p.Name, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => NuGetVersion.Parse(p.Version));
        }
    }

    string IApiOutlineOwner.Link(ApiModel api)
    {
        return Link.For(Package!.Value, api);
    }

    string IApiOutlineOwner.Link(ExtensionMethodModel api)
    {
        return Link.For(Package!.Value, api);
    }

    bool IApiOutlineOwner.IsIncluded(ApiModel api)
    {
        return _apiFilter.IsIncluded(api);
    }

    bool IApiOutlineOwner.IsSupported(ApiModel api)
    {
        return true;
    }
}