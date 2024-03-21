using ApisOfDotNet.Services;
using Microsoft.AspNetCore.Components;
using NuGet.Frameworks;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Pages;

public partial class FrameworkItem
{
    [Inject]
    public required CatalogService CatalogService { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Parameter]
    public string FrameworkName { get; set; }
    
    [SupplyParameterFromQuery(Name="p")]
    public string CurrentPage { get; set; }

    public FrameworkModel? Framework { get; set; }
    
    protected override void OnParametersSet()
    {
        Framework = CatalogService.Catalog.Frameworks
            .Where(a => string.Equals(a.Name, FrameworkName, StringComparison.OrdinalIgnoreCase))
            .Cast<FrameworkModel?>()
            .FirstOrDefault();

        if (string.IsNullOrEmpty(CurrentPage))
            CurrentPage = "assemblies";
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
}