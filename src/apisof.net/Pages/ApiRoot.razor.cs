#nullable enable

using ApisOfDotNet.Services;
using ApisOfDotNet.Shared;
using Microsoft.AspNetCore.Components;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Pages;

public partial class ApiRoot
{
    [Inject]
    public required CatalogService CatalogService { get; set; }

    [Inject]
    public required QueryManager QueryManager { get; set; }

    public required FilteredView<ApiModel> Items { get; set; }

    protected override void OnParametersSet()
    {
        var namespaceQuery = CatalogService.Catalog.RootApis.Order();
        Items = namespaceQuery.ToFilteredView(Filters.Match);
        Items.LinkFilterToQuery(QueryManager);
    }
}