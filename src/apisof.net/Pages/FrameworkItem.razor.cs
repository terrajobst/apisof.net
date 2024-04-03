#nullable enable
using ApisOfDotNet.Services;
using ApisOfDotNet.Shared;
using Microsoft.AspNetCore.Components;
using NuGet.Frameworks;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Pages;

public partial class FrameworkItem : IApiOutlineOwner
{
    [Inject]
    public required CatalogService CatalogService { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Parameter]
    public string? FrameworkName { get; set; }

    [Parameter]
    public string? ApiGuidText { get; set; }

    [SupplyParameterFromQuery(Name="p")]
    public string? CurrentPage { get; set; }

    public FrameworkModel? Framework { get; set; }
    
    public ApiOutlineData? OutlineData { get; set; }

    private ApiFilter _apiFilter = ApiFilter.Everything;

    protected override void OnParametersSet()
    {
        Framework = CatalogService.Catalog.Frameworks
            .Where(a => string.Equals(a.Name, FrameworkName, StringComparison.OrdinalIgnoreCase))
            .Cast<FrameworkModel?>()
            .FirstOrDefault();

        if (Framework is not null)
        {
            OutlineData = ApiGuidText is null
                ? ApiOutlineData.CreateRoot(Framework.Value)
                : ApiOutlineData.CreateNode(CatalogService.Catalog, ApiGuidText);
            
            _apiFilter = ApiFilter.ForFramework(Framework.Value);
        }
        
        if (string.IsNullOrEmpty(CurrentPage))
            CurrentPage = "apis";
    }
    
    public IEnumerable<FrameworkModel> Versions
    {
        get
        {
            if (Framework is null)
                return [];

            var nugetFx = NuGetFramework.Parse(Framework.Value.Name);
            
            return CatalogService.Catalog.Frameworks
                .Select(fx => (Framework: fx, NuGetFramework: NuGetFramework.Parse(fx.Name)))
                .Where(t => string.Equals(nugetFx.Framework, t.NuGetFramework.Framework, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.NuGetFramework.Version)
                .ThenBy(t => t.NuGetFramework.Platform)
                .ThenByDescending(t => t.NuGetFramework.PlatformVersion)
                .Select(t => t.Framework);
        }
    }

    string IApiOutlineOwner.Link(ApiModel api)
    {
        return Link.For(Framework!.Value, api);
    }

    string IApiOutlineOwner.Link(ExtensionMethodModel api)
    {
        return Link.For(Framework!.Value, api);
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