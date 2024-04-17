using Dapper;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Terrajobst.ApiCatalog;
using Terrajobst.ApiCatalog.Features;

var catalogPath = @"D:\apisofdotnet\apicatalog.dat";
var usageDataPath = @"D:\apisofdotnet\usageData.dat";
var usagesDbPath = @"D:\apisofdotnet\usages.db";

var catalog = await ApiCatalogModel.LoadAsync(catalogPath);
var usageData = FeatureUsageData.Load(usageDataPath);
var nugetSource = usageData.UsageSources.Single(u => u.Name == "nuget.org");

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = usagesDbPath,
    Mode = SqliteOpenMode.ReadWrite,
    Pooling = false
}.ToString();

var connection = new SqliteConnection(connectionString);
await connection.OpenAsync();

Console.Error.WriteLine("Creating index if they don't exist...");

await connection.ExecuteAsync(
    """
    CREATE INDEX IF NOT EXISTS IDX_Features_Guid_FeatureId
    ON Features (Guid, FeatureId);
    CREATE INDEX IF NOT EXISTS IDX_Usages_FeatureId_ReferenceUnitId
    ON Usages (FeatureId, ReferenceUnitId);
    """);

var scg = catalog.RootApis.Single(r => r.Name == "System.Collections.Generic");
var readOnlyInterfaces = scg
    .Children.Where(c => c.Kind == ApiKind.Interface &&
                         c.Name.StartsWith("IReadOnly"))
    .ToArray();
var readOnlyMembers = readOnlyInterfaces.SelectMany(i => i.Children)
                                        .Concat(readOnlyInterfaces)
                                        .Append(scg)
                                        .ToArray();

Console.WriteLine("# Default Interface Member (DIM) Usage");
Console.WriteLine();

var anyDimFeatureId = FeatureDefinition.DefinesAnyDefaultInterfaceMembers.FeatureId;
var anyDimPercent = usageData.GetUsage(nugetSource, anyDimFeatureId);

var (allPackages, packagesToReIndex) = await connection.QueryFirstAsync<(int Total, int NeedReindexing)>(
    """
    SELECT (SELECT COUNT(*) FROM ReferenceUnits) Total,
           (SELECT COUNT(*) FROM ReferenceUnits WHERE CollectorVersion < 2) NeedIndexing
    """);

var analyzedPackages = allPackages - packagesToReIndex;
var percentAnalyzed = (float)analyzedPackages / allPackages;

Console.WriteLine($"**{anyDimPercent:P3}** of packages declare any DIMs.");
Console.WriteLine();
Console.WriteLine($"This is based on **{percentAnalyzed:P0} of the latest stable & preview packages**,");
Console.WriteLine($"which is {analyzedPackages:N0} out of {allPackages:N0} packages.");

Console.WriteLine();
Console.WriteLine("## Usage among read-only interfaces");
Console.WriteLine();
Console.WriteLine($"| {"API",-88} | {"%",6} |");
Console.WriteLine($"|{new string('-',90)}|{new string('-',8)}|");

foreach (var member in readOnlyMembers.OrderBy(x => x.GetFullName()))
{
    var name = $"`{member.GetFullName()}`";
    var featureId = FeatureDefinition.DimUsage.GetFeatureId(member.Guid);
    var percentage = usageData.GetUsage(nugetSource, featureId)?.ToString("P3") ?? "N/A";
    Console.WriteLine($"| {name,-88} | {percentage,6} |");
}

Console.WriteLine();
Console.WriteLine("## Packages declaring DIMs of read-only interfaces");
Console.WriteLine();

var featureIds = readOnlyMembers.Select(m => FeatureDefinition.DimUsage.GetFeatureId(m.Guid)).ToArray();
var featureQuery = string.Join(", ", featureIds.Select(g => $"'{g.ToString().ToUpper()}'"));

var packages = await connection.QueryAsync<string>(
    $"""
     SELECT  DISTINCT
             r.Identifier
     FROM    Usages u
                 JOIN Features f on u.FeatureId = f.FeatureId
                 JOIN ReferenceUnits r on r.ReferenceUnitId = u.ReferenceUnitId
     WHERE   f.Guid IN ({featureQuery})
     """
);

var client = new HttpClient();
Console.Error.WriteLine("Querying package details from nuget.org...");

var packageIdAndVersions = packages.Select(s => s.Split('/'))
                                   .Select(a => (Id: a[0], Version: a[1]))
                                   .OrderBy(p => p.Id).ThenBy(p => p.Version);


Console.WriteLine($"| {"Package",-55} | {"Version",-25} | {"Link",-15} | {"Downloads",-15} | {"Unlisted",-8} | {"Last Updated",-15} |");
Console.WriteLine($"|{new string('-',57)}|{new string('-',27)}|{new string('-',17)}|{new string('-',17)}|{new string('-',10)}|{new string('-',17)}|");

var packageIndex = 0;

foreach (var (id, version) in packageIdAndVersions)
{
    var link = $"[Link][{packageIndex++}]";
    var (downloads, unlisted, lastUpdated) = await GetPackageInfoAsync(id, version);
    var unlistedText = unlisted ? "Yes" : "No";
    var lastUpdatedText = lastUpdated?.ToString("d");
    Console.WriteLine($"| {id,-55} | {version,-25} | {link,-15} | {downloads,15} | {unlistedText,-8} | {lastUpdatedText,-15} |");
}

Console.WriteLine();

packageIndex = 0;

foreach (var (id, version) in packageIdAndVersions)
{
    var url = GetUrl(id, version);
    Console.WriteLine($"[{packageIndex++}]: {url}");
}

static string GetUrl(string id, string version)
{
    return $"https://nuget.org/packages/{id}/{version}";
}

async Task<PackageInfo> GetPackageInfoAsync(string id, string version)
{
    var url = GetUrl(id, version);
    var content = await client.GetStringAsync(url);
    var downloads = GetDownloads(content);
    var isMarkedAsUnlisted = IsMarkedAsUnlisted(content);
    var lastUpdated = GetLastUpdated(content);
    return new PackageInfo(downloads, isMarkedAsUnlisted, lastUpdated);
}

string? GetDownloads(string content)
{
    var match = Regex.Match(content,
        """
        \<span class="download-info-content"\>([^<]+)\</span\>
        """);

    if (!match.Success)
        return null;

    return match.Groups[1].Value;
}

bool IsMarkedAsUnlisted(string content)
{
    return content.Contains("The owner has unlisted this package");
}

DateTime? GetLastUpdated(string content)
{
    DateTime? result = null;
    
    foreach (Match match in Regex.Matches(content, @"data-datetime=""([^""]+)"""))
    {
        var text = match.Groups[1].Value;
        if (DateTime.TryParse(text, out var d))
        {
            if (result is null)
                result = d;
            else
                result = d > result ? d : result;
        }
    }

    return result;
}

record struct PackageInfo(string? Downloads, bool Unlisted, DateTime? lastUpdated);