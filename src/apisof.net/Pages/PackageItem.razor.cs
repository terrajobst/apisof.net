using ApisOfDotNet.Services;
using Microsoft.AspNetCore.Components;
using NuGet.Frameworks;
using NuGet.Versioning;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Pages;

public partial class PackageItem
{
    [Inject]
    public required CatalogService CatalogService { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Parameter]
    public string PackageName { get; set; }

    [SupplyParameterFromQuery(Name = "p")]
    public string CurrentPage { get; set; }

    public PackageModel? Package { get; set; }

    protected override void OnParametersSet()
    {
        PackageName ??= "";
        Package = PackageName.Split('/') switch {
            [var name] =>
                CatalogService.Catalog.Packages
                    .Where(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(a => NuGetVersion.Parse(a.Version))
                    .Cast<PackageModel?>()
                    .FirstOrDefault(),
            [var name, var version] =>
                CatalogService.Catalog.Packages
                    .Where(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(a.Version, version, StringComparison.OrdinalIgnoreCase))
                    .Cast<PackageModel?>()
                    .FirstOrDefault(),
            _ => null
        };

        if (string.IsNullOrEmpty(CurrentPage))
            CurrentPage = "assemblies";
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
}