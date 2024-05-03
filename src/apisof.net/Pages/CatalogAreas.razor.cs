using ApisOfDotNet.Services;
using Microsoft.AspNetCore.Components;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Pages;

public partial class CatalogAreas
{
    [Inject]
    public required CatalogService CatalogService { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Inject]
    public required LinkService Link { get; set; }

    [Parameter]
    public string? AreaName { get; set; }

    private IEnumerable<Area> Areas => Area.All;

    private Area? Area { get; set; }

    public IEnumerable<ApiModel> GetRootApis()
    {
        if (Area is null)
            return Enumerable.Empty<ApiModel>();

        return CatalogService.Catalog.RootApis.Where(a => Area.Include.Any(m => m.Matches(a))).Order();
    }

    protected override void OnParametersSet()
    {
        Area = Areas.FirstOrDefault(a => string.Equals(a.Name, AreaName, StringComparison.OrdinalIgnoreCase));
    }
}