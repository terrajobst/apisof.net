using ApisOfDotNet.Services;
using Microsoft.AspNetCore.Components;
using NuGet.Frameworks;
using NuGet.Versioning;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Pages;

public partial class AssemblyItem
{
    [Inject]
    public required CatalogService CatalogService { get; set; }
    
    [Inject]
    public required NavigationManager NavigationManager { get; set; }
    
    [Parameter]
    public required string GuidText { get; set; }

    [SupplyParameterFromQuery(Name="p")]
    public string CurrentPage { get; set; }

    public AssemblyModel? Assembly { get; set; }

    protected override void OnParametersSet()
    {
        Assembly = Guid.TryParse(GuidText, out var guid)
            ? CatalogService.Catalog.Assemblies
                .Where(a => a.Guid == guid)
                .Cast<AssemblyModel?>()
                .FirstOrDefault()
            : null;

        if (string.IsNullOrEmpty(CurrentPage))
        {
            CurrentPage = Frameworks.Any()
                            ? "frameworks"
                            : Packages.Any()
                                ? "packages"
                                : "versions";
        }
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
}