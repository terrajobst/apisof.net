using Terrajobst.ApiCatalog;
using Terrajobst.ApiCatalog.Features;

var catalogPath = @"D:\apisofdotnet\apicatalog.dat";
var usageDataPath = @"D:\apisofdotnet\usageData.dat";

var catalog = await ApiCatalogModel.LoadAsync(catalogPath);
var usageData = FeatureUsageData.Load(usageDataPath);
var nugetSource = usageData.UsageSources.Single(u => u.Name == "nuget.org");

var scg = catalog.RootApis.Single(r => r.Name == "System.Collections.Generic");
var readOnlyInterfaces = scg
    .Children.Where(c => c.Kind == ApiKind.Interface &&
                         c.Name.StartsWith("IReadOnly"))
    .Concat(scg.Children.Where(c => c.Kind == ApiKind.Interface &&
                                    c.Name == "IEnumerable<T>"))
    .ToArray();
var readOnlyMembers = readOnlyInterfaces.SelectMany(i => i.Children)
                                        .Concat(readOnlyInterfaces)
                                        .Append(scg)
                                        .ToArray();

Console.WriteLine("# Default Interface Member (DIM) Usage");
Console.WriteLine();

var anyDimFeatureId = FeatureDefinition.DefinesAnyDefaultInterfaceMembers.FeatureId;
var anyDimPercent = usageData.GetUsage(nugetSource, anyDimFeatureId);

var allPackages = 681930;
var packagesToReIndex = 177772;
var analyzedPackages = allPackages - packagesToReIndex;
var percentAnalyzed = (float)analyzedPackages / allPackages;

Console.WriteLine($"**{anyDimPercent:P3}** of packages declare any DIMs.");
Console.WriteLine();
Console.WriteLine($"This is based on **{percentAnalyzed:P0} of the latest stable & preview packages**,");
Console.WriteLine($"which is {analyzedPackages:N0} out of {allPackages:N0} packages.");

Console.WriteLine();
Console.WriteLine("## Usage among read-only interfaces:");
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
