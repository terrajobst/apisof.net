using Terrajobst.ApiCatalog;

var catalogFileName = Path.Join(Path.GetDirectoryName(Environment.ProcessPath), "catalog.bin");

if (!File.Exists(catalogFileName))
{
    Console.WriteLine("Downloading catalog...");
    await ApiCatalogModel.DownloadFromWebAsync(catalogFileName);
}

Console.WriteLine("Reading catalog...");
var catalog = await ApiCatalogModel.LoadAsync(catalogFileName);

CheckForMissingArea(catalog.RootApis);

foreach (var area in Area.All)
{
    var count = catalog.RootApis.Count(a => area.Include.Any(m => m.Matches(a)));

    Console.WriteLine($"{area.Name} ({count})");
}

static void CheckForMissingArea(IEnumerable<ApiModel> apis)
{
    foreach (var api in apis.Order())
    {
        if (IsOnlyDefinedInNetCore2(api))
            continue;

        if (IsOnlyDefinedInIrrelevantFramework(api))
            continue;

        var hasAnyAreas = Area.All.Any(a => a.Include.Any(m => m.Matches(api)));
        if (hasAnyAreas)
            continue;

        Console.WriteLine(api);
    }
}

static bool IsOnlyDefinedInNetCore2(ApiModel model)
{
    foreach (var d in model.Declarations)
    {
        if (d.Assembly.Packages.Any())
            return false;

        foreach (var fx in d.Assembly.Frameworks)
        {
            if (fx.NuGetFramework.Version.Major != 2)
                return false;

            if (!string.Equals(fx.NuGetFramework.Framework, ".NETCoreApp", StringComparison.OrdinalIgnoreCase))
                return false;
        }
    }

    return true;
}

static bool IsOnlyDefinedInIrrelevantFramework(ApiModel model)
{
    foreach (var d in model.Declarations)
    {
        if (d.Assembly.Packages.Any())
            return false;

        foreach (var fx in d.Assembly.Frameworks)
        {
            if (fx.NuGetFramework.IsRelevantForCatalog())
                return false;
        }
    }

    return true;
}
