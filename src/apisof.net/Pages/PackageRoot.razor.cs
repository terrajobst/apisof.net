using ApisOfDotNet.Services;
using ApisOfDotNet.Shared;
using Microsoft.AspNetCore.Components;
using NuGet.Versioning;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Pages;

public partial class PackageRoot
{
    [Inject]
    public required CatalogService CatalogService { get; set; }

    [Inject]
    public required QueryManager QueryManager { get; set; }

    public required FilteredView<PackageModel> Items { get; set; }

    protected override void OnParametersSet()
    {
        var packageQuery = CatalogService.Catalog
            .Packages
            .GroupBy(p => p.Name)
            .Select(g => g.MaxBy(p => NuGetVersion.Parse(p.Version)))
            .OrderBy(p => p.Name);
        Items = packageQuery.ToFilteredView(Filters.Match);
        Items.LinkFilterToQuery(QueryManager);
    }
}