using ApisOfDotNet.Services;
using ApisOfDotNet.Shared;
using Microsoft.AspNetCore.Components;
using NuGet.Frameworks;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Pages;

public partial class FrameworkRoot
{
    [Inject]
    public required CatalogService CatalogService { get; set; }

    [Inject]
    public required QueryManager QueryManager { get; set; }

    public required FilteredView<FrameworkModel> Items { get; set; }

    protected override void OnParametersSet()
    {
        var frameworkQuery = CatalogService.Catalog
            .Frameworks
            .Select(fx => (Framework: fx, NuGetFramework: NuGetFramework.Parse(fx.Name)))
            .OrderBy(t => t.NuGetFramework.Framework)
            .ThenBy(t => t.NuGetFramework.Version)
            .ThenBy(t => t.NuGetFramework.Platform)
            .ThenBy(t => t.NuGetFramework.PlatformVersion)
            .Select(t => t.Framework);
        Items = frameworkQuery.ToFilteredView(Filters.Match);
        Items.LinkFilterToQuery(QueryManager);
    }
}