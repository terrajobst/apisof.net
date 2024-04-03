#nullable enable
using ApisOfDotNet.Services;
using ApisOfDotNet.Shared;
using Microsoft.AspNetCore.Components;
using NuGet.Frameworks;
using NuGet.Versioning;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Pages;

public partial class AssemblyItem : IApiOutlineOwner
{
    [Inject]
    public required CatalogService CatalogService { get; set; }
    
    [Inject]
    public required NavigationManager NavigationManager { get; set; }
    
    [Parameter]
    public required string GuidText { get; set; }

    [Parameter]
    public string? ApiGuidText { get; set; }

    [SupplyParameterFromQuery(Name="p")]
    public string? CurrentPage { get; set; }

    public AssemblyModel? Assembly { get; set; }
    
    public ApiOutlineData? OutlineData { get; set; }

    private ApiFilter _apiFilter = ApiFilter.Everything;

    protected override void OnParametersSet()
    {
        Assembly = Guid.TryParse(GuidText, out var guid)
            ? CatalogService.Catalog.Assemblies
                .Where(a => a.Guid == guid)
                .Cast<AssemblyModel?>()
                .FirstOrDefault()
            : null;

        if (Assembly is not null)
        {
            OutlineData = ApiGuidText is null
                ? ApiOutlineData.CreateRoot(Assembly.Value.RootApis)
                : ApiOutlineData.CreateNode(CatalogService.Catalog, ApiGuidText);
            
            _apiFilter = ApiFilter.ForAssembly(Assembly.Value);
        }

        if (string.IsNullOrEmpty(CurrentPage))
            CurrentPage = "apis";
    }

    public IEnumerable<FrameworkModel> Frameworks
    {
        get
        {
            if (Assembly is null)
                return [];

            return Assembly.Value.Frameworks
                .Select(fx => (Frameworks: fx, NuGetFramework: NuGetFramework.Parse(fx.Name)))
                .OrderBy(fx => fx.NuGetFramework.Framework)
                .ThenByDescending(fx => fx.NuGetFramework.Version)
                .ThenBy(fx => fx.NuGetFramework.Platform)
                .ThenByDescending(fx => fx.NuGetFramework.PlatformVersion)
                .Select(fx => fx.Frameworks);
        }
    }
    
    public IEnumerable<PackageModel> Packages
    {
        get
        {
            if (Assembly is null)
                return [];

            return Assembly.Value.Packages
                .Select(t => t.Package)
                .Distinct()
                .OrderBy(p => p.Name)
                .ThenBy(p => NuGetVersion.Parse(p.Version));
        }
    }
    
    public IEnumerable<AssemblyModel> Assemblies
    {
        get
        {
            if (Assembly is null)
                return [];

            return CatalogService.Catalog.Assemblies
                .Where(a => string.Equals(a.Name, Assembly.Value.Name, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(a => System.Version.Parse(a.Version))
                .ThenBy(a => a.Guid);
        }
    }

    string IApiOutlineOwner.Link(ApiModel api)
    {
        return Link.For(Assembly!.Value, api);
    }

    string IApiOutlineOwner.Link(ExtensionMethodModel api)
    {
        return Link.For(Assembly!.Value, api);
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