using ApisOfDotNet.Services;
using ApisOfDotNet.Shared;
using Microsoft.AspNetCore.Components;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Pages;

public partial class AssemblyRoot
{
    [Inject]
    public required CatalogService CatalogService { get; set; }

    [Inject]
    public required QueryManager QueryManager { get; set; }

    public required FilteredView<AssemblyModel> Items { get; set; }
    
    protected override void OnParametersSet()
    {
        var assemblyQuery = CatalogService.Catalog
            .Assemblies
            .GroupBy(a => a.Name)
            .Select(g => g.MaxBy(a => System.Version.Parse(a.Version)))
            .OrderBy(a => a.Name);
        
        Items = assemblyQuery.ToFilteredView(Filters.Match);
        Items.LinkFilterToQuery(QueryManager);
    }
}