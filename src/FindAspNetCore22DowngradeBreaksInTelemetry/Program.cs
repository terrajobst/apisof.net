using Terrajobst.ApiCatalog.Features;

var usageDataPath = @"D:\Code\apisofdotnet\usageData.dat";
var outputPath = @"D:\\Code\\aspnet22-users\\aspnet22-usage.csv";

Console.WriteLine("Loading usage data...");
var usageData = FeatureUsageData.Load(usageDataPath);

Console.WriteLine("Loading breaking changes...");
var data = ApiDatabase.Load();

using var writer = new CsvWriter(outputPath);
writer.Write("Namespace");
writer.Write("Type");
writer.Write("Member");

var sourcesWitData = usageData.UsageSources
    .Where(s => data.Entries.Any(e => usageData.GetUsage(s, e.Guid) is not null))
    .ToArray();

foreach (var source in sourcesWitData)
    writer.Write(source.Name);

writer.WriteLine();

foreach (var entry in data.Entries.OrderBy(e => e.NamespaceName)
                                  .ThenBy(e => e.TypeName)
                                  .ThenBy(e => e.MemberName))
{
    if (!usageData.GetUsage(entry.Guid).Any())
        continue;

    writer.Write(entry.NamespaceName);
    writer.Write(entry.TypeName);
    writer.Write(entry.MemberName);

    foreach (var source in sourcesWitData)
    {
        var percentage = usageData.GetUsage(source, entry.Guid);
        writer.Write(percentage is null ? "" : percentage.Value.ToString("P5"));
    }

    writer.WriteLine();
}