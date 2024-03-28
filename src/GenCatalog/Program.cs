using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using Azure.Core;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration.UserSecrets;
using Terrajobst.ApiCatalog;

namespace GenCatalog;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        static bool IsType(ApiModel api)
        {
            return api.Kind.IsType();
        }

        static bool IsMethod(ApiModel api)
        {
            return api.Kind switch {
                ApiKind.Constructor => true,
                ApiKind.PropertyGetter => true,
                ApiKind.PropertySetter => true,
                ApiKind.Method => true,
                ApiKind.Operator => true,
                ApiKind.EventAdder => true,
                ApiKind.EventRemover => true,
                ApiKind.EventRaiser => true,
                _ => false
            };
        }

        static bool IsField(ApiModel api)
        {
            return api.Kind is ApiKind.Field or ApiKind.EnumItem;
        }

        static bool IsProperty(ApiModel api)
        {
            return api.Kind == ApiKind.Property;
        }

        static bool IsEvent(ApiModel api)
        {
            return api.Kind == ApiKind.Event;
        }

        static int GetParameterCount(ApiModel api)
        {
            if (!IsMethod(api))
                return 0;

            var name = api.Name;
            var openParen = name.IndexOf('(');
            var closeParen = name.IndexOf(')');

            if (openParen < 0 && closeParen < 0)
                return 0;
            
            if (openParen < 0 || closeParen < 0)
                return 0;

            if (closeParen == openParen + 1)
                return 0;

            var result = 1;

            for (var i = openParen + 1; i < closeParen; i++)
            {
                if (name[i] == ',')
                    result++;
            }

            return result;
        }

        static int GetPropertyMapCount(ApiModel api)
        {
            if (!IsProperty(api))
                return 0;

            return api.Children.Count();
        }

        static int GetEventMapCount(ApiModel api)
        {
            if (!IsEvent(api))
                return 0;

            return api.Children.Count();
        }

        static int GetGenericParameterCount(ApiModel api)
        {
            if (!IsType(api) && !IsMethod(api))
                return 0;

            var name = api.Name;
            var openParen = name.IndexOf('<');
            var closeParen = name.IndexOf('>');

            if (openParen < 0 || closeParen < 0)
                return 0;

            var result = 1;

            for (var i = openParen + 1; i < closeParen; i++)
            {
                if (name[i] == ',')
                    result++;
            }

            return result;
        }

        var catalog = await ApiCatalogModel.LoadAsync(@"C:\Users\immol\Downloads\Catalog\apicatalog.dat");

        var childArraySizes = catalog.AllApis.Where(a => a.Children.Any())
                                             .Sum(a => 4 + 4 * a.Children.Count());

        var groups = catalog.AllApis.GroupBy(a => a.Kind)
            .Select(g => (Kind: g.Key, Count: g.Count()))
            .ToArray();
        
        foreach (var g in groups)
            Console.WriteLine(g);

        const int TypeRowSize = 4 + 4 + 4 + 4 + 4;
        const int MethodRowSize = 4 + 4 + 4;
        const int ParameterRowSize = 4 + 2 + 4;
        const int FieldRowSize = 4 + 4;
        const int PropertyRowSize = 4 + 4;
        const int PropertyMapRowSize = 4 + 4;
        const int EventRowSize = 4 + 4;
        const int EventMapRowSize = 4 + 4;
        const int GenericParameterRowSize = 4 + 2 + 4;

        const int NamespaceDeclarationRowSize = 4;
        const int TypeDeclarationRowSize = 4 + 4;
        const int MethodDeclarationRowSize = 4 + 4;
        const int ParameterDeclarationRowSize = 4 + 4;
        const int FieldDeclarationRowSize = 4 + 4;
        const int PropertyDeclarationRowSize = 4 + 4;
        const int EventDeclarationRowSize = 4 + 4;
        const int GenericParameterDeclarationRowSize = 4 + 4;
        const int GenericParameterConstraintDeclarationRowSize = 4 + 4;
        const int CustomAttributeDeclarationsRowSize = 4 + 4 + 4 + 4;
        
        var numberOfTypes = catalog.AllApis.Count(IsType);
        var numberOfMethods = catalog.AllApis.Count(IsMethod);
        var numberOfParameters = catalog.AllApis.Sum(GetParameterCount);
        var numberOfFields = catalog.AllApis.Count(IsField);
        var numberOfProperties = catalog.AllApis.Count(IsProperty);
        var numberOfPropertyMaps = catalog.AllApis.Sum(GetPropertyMapCount);
        var numberOfEvents = catalog.AllApis.Count(IsEvent);
        var numberOfEventMaps = catalog.AllApis.Sum(GetEventMapCount);
        var numberOfGenericParameters = catalog.AllApis.Sum(GetGenericParameterCount);
        
        var numberOfTypeDeclarations = catalog.AllApis.Where(IsType).Sum(a => a.Declarations.Count());
        var numberOfMethodDeclarations = catalog.AllApis.Where(IsMethod).Sum(a => a.Declarations.Count());
        var numberOfParameterDeclarations = catalog.AllApis.Sum(a => GetParameterCount(a) * a.Declarations.Count());
        var numberOfFieldDeclarations = catalog.AllApis.Where(IsField).Sum(a => a.Declarations.Count());
        var numberOfPropertyDeclarations = catalog.AllApis.Where(IsProperty).Sum(a => a.Declarations.Count());
        var numberOfEventDeclarations = catalog.AllApis.Where(IsEvent).Sum(a => a.Declarations.Count());
        var numberOfGenericParameterDeclarations = catalog.AllApis.Sum(a => GetGenericParameterCount(a) * a.Declarations.Count());
        
        var TypeTableSize = numberOfTypes * TypeRowSize;
        var MethodTableSize = numberOfMethods * MethodRowSize;
        var ParameterTableSize = numberOfParameters * ParameterRowSize;
        var FieldTableSize = numberOfFields * FieldRowSize;
        var PropertyTableSize = numberOfProperties * PropertyRowSize;
        var PropertyMapTableSize = numberOfPropertyMaps * PropertyMapRowSize;
        var EventTableSize = numberOfEvents * EventRowSize;
        var EventMapTableSize = numberOfEventMaps * EventMapRowSize;
        var GenericParameterTableSize = numberOfGenericParameters * GenericParameterRowSize;

        var TypeDeclarationTableSize = numberOfTypeDeclarations * TypeDeclarationRowSize;
        var MethodDeclarationTableSize = numberOfMethodDeclarations * MethodDeclarationRowSize;
        var ParameterDeclarationTableSize = numberOfParameterDeclarations * ParameterDeclarationRowSize;
        var FieldDeclarationTableSize = numberOfFieldDeclarations * FieldDeclarationRowSize;
        var PropertyDeclarationTableSize = numberOfPropertyDeclarations * PropertyDeclarationRowSize;
        var EventDeclarationTableSize = numberOfEventDeclarations * EventDeclarationRowSize;
        var GenericParameterDeclarationTableSize = numberOfGenericParameterDeclarations * GenericParameterDeclarationRowSize;

        var TotalSize = TypeTableSize + MethodTableSize + ParameterTableSize + FieldTableSize + PropertyTableSize + PropertyMapTableSize + EventTableSize + EventMapTableSize + GenericParameterTableSize + TypeDeclarationTableSize + MethodDeclarationTableSize + ParameterDeclarationTableSize + FieldDeclarationTableSize + PropertyDeclarationTableSize + EventDeclarationTableSize + GenericParameterDeclarationTableSize;

        Console.WriteLine($"#Types                        : {numberOfTypes,12:N0}");
        Console.WriteLine($"#Methods                      : {numberOfMethods,12:N0}");
        Console.WriteLine($"#Parameters                   : {numberOfParameters,12:N0}");
        Console.WriteLine($"#Fields                       : {numberOfFields,12:N0}");
        Console.WriteLine($"#Properties                   : {numberOfProperties,12:N0}");
        Console.WriteLine($"#PropertyMaps                 : {numberOfPropertyMaps,12:N0}");
        Console.WriteLine($"#Events                       : {numberOfEvents,12:N0}");
        Console.WriteLine($"#EventMaps                    : {numberOfEventMaps,12:N0}");
        Console.WriteLine($"#GenericParameters            : {numberOfGenericParameters,12:N0}");
        Console.WriteLine($"#TypeDeclarations             : {numberOfTypeDeclarations,12:N0}");
        Console.WriteLine($"#MethodDeclarations           : {numberOfMethodDeclarations,12:N0}");
        Console.WriteLine($"#ParameterDeclarations        : {numberOfParameterDeclarations,12:N0}");
        Console.WriteLine($"#FieldDeclarations            : {numberOfFieldDeclarations,12:N0}");
        Console.WriteLine($"#PropertyDeclarations         : {numberOfPropertyDeclarations,12:N0}");
        Console.WriteLine($"#EventDeclarations            : {numberOfEventDeclarations,12:N0}");
        Console.WriteLine($"#GenericParameterDeclarations : {numberOfGenericParameterDeclarations,12:N0}");
        Console.WriteLine();
        Console.WriteLine($"TypeTableSize                        : {TypeTableSize,12:N0}");
        Console.WriteLine($"MethodTableSize                      : {MethodTableSize,12:N0}");
        Console.WriteLine($"ParameterTableSize                   : {ParameterTableSize,12:N0}");
        Console.WriteLine($"FieldTableSize                       : {FieldTableSize,12:N0}");
        Console.WriteLine($"PropertyTableSize                    : {PropertyTableSize,12:N0}");
        Console.WriteLine($"PropertyMapTableSize                 : {PropertyMapTableSize,12:N0}");
        Console.WriteLine($"EventTableSize                       : {EventTableSize,12:N0}");
        Console.WriteLine($"EventMapTableSize                    : {EventMapTableSize,12:N0}");
        Console.WriteLine($"GenericParameterTableSize            : {GenericParameterTableSize,12:N0}");
        Console.WriteLine();
        Console.WriteLine($"TypeDeclarationTableSize             : {TypeDeclarationTableSize,12:N0}");
        Console.WriteLine($"MethodDeclarationTableSize           : {MethodDeclarationTableSize,12:N0}");
        Console.WriteLine($"ParameterDeclarationTableSize        : {ParameterDeclarationTableSize,12:N0}");
        Console.WriteLine($"FieldDeclarationTableSize            : {FieldDeclarationTableSize,12:N0}");
        Console.WriteLine($"PropertyDeclarationTableSize         : {PropertyDeclarationTableSize,12:N0}");
        Console.WriteLine($"EventDeclarationTableSize            : {EventDeclarationTableSize,12:N0}");
        Console.WriteLine($"GenericParameterDeclarationTableSize : {GenericParameterDeclarationTableSize,12:N0}");
        Console.WriteLine();
        Console.WriteLine($"TotalSize                            : {TotalSize,12:N0}");

        
        return 0;
        
        if (args.Length > 1)
        {
            var exeName = Path.GetFileNameWithoutExtension(typeof(Program).Assembly.Location);
            Console.Error.Write("error: incorrect number of arguments");
            Console.Error.Write($"usage: {exeName} [<download-directory>]");
            return -1;
        }

        var rootPath = args.Length == 1
            ? args[0]
            : Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Catalog");

        var success = true;

        try
        {
            await RunAsync(rootPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            success = false;
        }

        try
        {
            await UploadSummaryAsync(success);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            success = false;
        }

        if (success)
            await PostToGenCatalogWebHook();

        return success ? 0 : -1;
    }

    private static async Task RunAsync(string rootPath)
    {
        var indexPath = Path.Combine(rootPath, "index");
        var indexFrameworksPath = Path.Combine(indexPath, "frameworks");
        var indexPackagesPath = Path.Combine(indexPath, "packages");
        var packagesPath = Path.Combine(rootPath, "packages");
        var packageListPath = Path.Combine(packagesPath, "packages.xml");
        var frameworksPath = Path.Combine(rootPath, "frameworks");
        var packsPath = Path.Combine(rootPath, "packs");
        var apiUsagesPath = Path.Combine(rootPath, "api-usages");
        var nugetUsagesPath = Path.Combine(apiUsagesPath, "nuget.org.tsv");
        var plannerUsagesPath = Path.Combine(apiUsagesPath, "Upgrade Planner.tsv");
        var netfxCompatLabPath = Path.Combine(apiUsagesPath, "NetFx Compat Lab.tsv");
        var suffixTreePath = Path.Combine(rootPath, "suffixTree.dat");
        var catalogModelPath = Path.Combine(rootPath, "apicatalog.dat");

        var stopwatch = Stopwatch.StartNew();

        await DownloadArchivedPlatformsAsync(frameworksPath);
        await DownloadPackagedPlatformsAsync(frameworksPath, packsPath);
        await DownloadDotnetPackageListAsync(packageListPath);
        await DownloadNuGetUsages(nugetUsagesPath);
        await DownloadPlannerUsages(plannerUsagesPath);
        await DownloadNetFxCompatLabUsages(netfxCompatLabPath);
        await GeneratePlatformIndexAsync(frameworksPath, indexFrameworksPath);
        await GeneratePackageIndexAsync(packageListPath, packagesPath, indexPackagesPath, frameworksPath);
        await GenerateCatalogAsync(indexFrameworksPath, indexPackagesPath, apiUsagesPath, catalogModelPath);
        await GenerateSuffixTreeAsync(catalogModelPath, suffixTreePath);
        await UploadCatalogModelAsync(catalogModelPath);
        await UploadSuffixTreeAsync(suffixTreePath);

        Console.WriteLine($"Completed in {stopwatch.Elapsed}");
        Console.WriteLine($"Peak working set: {Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024):N2} MB");
    }

    private static string GetAzureStorageConnectionString()
    {
        var result = Environment.GetEnvironmentVariable("AzureStorageConnectionString");
        if (string.IsNullOrEmpty(result))
        {
            var secrets = Secrets.Load();
            result = secrets?.AzureStorageConnectionString;
        }

        if (string.IsNullOrEmpty(result))
            throw new Exception("Cannot retrieve connection string for Azure blob storage. You either need to define an environment variable or a user secret.");

        return result;
    }

    private static string? GetDetailsUrl()
    {
        var serverUrl = Environment.GetEnvironmentVariable("GITHUB_SERVER_URL");
        var repository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
        var runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID");
        return $"{serverUrl}/{repository}/actions/runs/{runId}";
    }

    private static async Task DownloadArchivedPlatformsAsync(string archivePath)
    {
        var connectionString = GetAzureStorageConnectionString();
        var container = "archive";
        var containerClient = new BlobContainerClient(connectionString, container, options: GetBlobOptions());

        await foreach (var blob in containerClient.GetBlobsAsync())
        {
            var nameWithoutExtension = Path.ChangeExtension(blob.Name, null);
            var localDirectory = Path.Combine(archivePath, nameWithoutExtension);
            if (!Directory.Exists(localDirectory))
            {
                Console.WriteLine($"Downloading {nameWithoutExtension}...");
                var blobClient = new BlobClient(connectionString, container, blob.Name, options: GetBlobOptions());
                using var blobStream = await blobClient.OpenReadAsync();
                using var archive = new ZipArchive(blobStream, ZipArchiveMode.Read);
                archive.ExtractToDirectory(localDirectory);
            }
        }
    }

    private static async Task DownloadPackagedPlatformsAsync(string archivePath, string packsPath)
    {
        await FrameworkDownloader.DownloadAsync(archivePath, packsPath);
    }

    private static async Task DownloadDotnetPackageListAsync(string packageListPath)
    {
        if (!File.Exists(packageListPath))
            await DotnetPackageIndex.CreateAsync(packageListPath);
    }

    private static async Task DownloadNuGetUsages(string nugetUsagesPath)
    {
        if (File.Exists(nugetUsagesPath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(nugetUsagesPath)!);

        Console.WriteLine("Downloading NuGet usages...");

        var connectionString = GetAzureStorageConnectionString();
        var blobClient = new BlobClient(connectionString, "usage", "usages.tsv", options: GetBlobOptions());
        var props = await blobClient.GetPropertiesAsync();
        var lastModified = props.Value.LastModified;
        await blobClient.DownloadToAsync(nugetUsagesPath);
        File.SetLastWriteTimeUtc(nugetUsagesPath, lastModified.UtcDateTime);
    }

    private static async Task DownloadPlannerUsages(string plannerUsagesPath)
    {
        if (File.Exists(plannerUsagesPath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(plannerUsagesPath)!);

        Console.WriteLine("Downloading Planner usages...");

        var connectionString = GetAzureStorageConnectionString();
        var blobClient = new BlobClient(connectionString, "usage", "usages-planner.tsv", options: GetBlobOptions());
        var props = await blobClient.GetPropertiesAsync();
        var lastModified = props.Value.LastModified;
        await blobClient.DownloadToAsync(plannerUsagesPath);
        File.SetLastWriteTimeUtc(plannerUsagesPath, lastModified.UtcDateTime);
    }

    private static async Task DownloadNetFxCompatLabUsages(string netfxCompatLabPath)
    {
        if (File.Exists(netfxCompatLabPath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(netfxCompatLabPath)!);

        Console.WriteLine("Downloading NetFx Compat Lab usages...");

        var connectionString = GetAzureStorageConnectionString();
        var blobClient = new BlobClient(connectionString, "usage", "netfxcompatlab.tsv", options: GetBlobOptions());
        var props = await blobClient.GetPropertiesAsync();
        var lastModified = props.Value.LastModified;
        await blobClient.DownloadToAsync(netfxCompatLabPath);
        File.SetLastWriteTimeUtc(netfxCompatLabPath, lastModified.UtcDateTime);
    }

    private static Task GeneratePlatformIndexAsync(string frameworksPath, string indexFrameworksPath)
    {
        var frameworkResolvers = new FrameworkProvider[]
        {
            new ArchivedFrameworkProvider(frameworksPath),
            new PackBasedFrameworkProvider(frameworksPath)
        };

        var frameworks = frameworkResolvers.SelectMany(r => r.Resolve())
            .OrderBy(t => t.FrameworkName);
        var reindex = false;

        Directory.CreateDirectory(indexFrameworksPath);

        foreach (var (frameworkName, paths) in frameworks)
        {
            var path = Path.Join(indexFrameworksPath, $"{frameworkName}.xml");
            var alreadyIndexed = !reindex && File.Exists(path);

            if (alreadyIndexed)
            {
                Console.WriteLine($"{frameworkName} already indexed.");
            }
            else
            {
                Console.WriteLine($"Indexing {frameworkName}...");
                var frameworkEntry = FrameworkIndexer.Index(frameworkName, paths);
                using (var stream = File.Create(path))
                    frameworkEntry.Write(stream);
            }
        }

        return Task.CompletedTask;
    }

    private static async Task GeneratePackageIndexAsync(string packageListPath, string packagesPath, string indexPackagesPath, string frameworksPath)
    {
        var frameworkLocators = new FrameworkLocator[]
        {
            new ArchivedFrameworkLocator(frameworksPath),
            new PackBasedFrameworkLocator(frameworksPath),
            new PclFrameworkLocator(frameworksPath)
        };

        Directory.CreateDirectory(packagesPath);
        Directory.CreateDirectory(indexPackagesPath);

        var document = XDocument.Load(packageListPath);
        Directory.CreateDirectory(packagesPath);

        var packages = document.Root!.Elements("package")
            .Select(e => (
                Id: e.Attribute("id")!.Value,
                Version: e.Attribute("version")!.Value))
            .ToArray();

        var nightlies = new NuGetFeed(NuGetFeeds.NightlyLatest);
        var nuGetOrg = new NuGetFeed(NuGetFeeds.NuGetOrg);
        var nugetStore = new NuGetStore(packagesPath, nightlies, nuGetOrg);
        var packageIndexer = new PackageIndexer(nugetStore, frameworkLocators);

        var retryIndexed = false;
        var retryDisabled = false;
        var retryFailed = false;

        foreach (var (id, version) in packages)
        {
            var path = Path.Join(indexPackagesPath, $"{id}-{version}.xml");
            var disabledPath = Path.Join(indexPackagesPath, $"{id}-all.disabled");
            var failedVersionPath = Path.Join(indexPackagesPath, $"{id}-{version}.failed");

            var alreadyIndexed = !retryIndexed && File.Exists(path) ||
                                 !retryDisabled && File.Exists(disabledPath) ||
                                 !retryFailed && File.Exists(failedVersionPath);

            if (alreadyIndexed)
            {
                if (File.Exists(path))
                    Console.WriteLine($"Package {id} {version} already indexed.");

                if (File.Exists(disabledPath))
                    nugetStore.DeleteFromCache(id, version);
            }
            else
            {
                Console.WriteLine($"Indexing {id} {version}...");
                try
                {
                    var packageEntry = await packageIndexer.Index(id, version);
                    if (packageEntry is null)
                    {
                        Console.WriteLine($"Not a library package.");
                        File.WriteAllText(disabledPath, string.Empty);
                        nugetStore.DeleteFromCache(id, version);
                    }
                    else
                    {
                        using (var stream = File.Create(path))
                            packageEntry.Write(stream);

                        File.Delete(disabledPath);
                        File.Delete(failedVersionPath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed: {ex}");
                    File.Delete(disabledPath);
                    File.Delete(path);
                    File.WriteAllText(failedVersionPath, ex.ToString());
                }
            }
        }
    }

    private static async Task GenerateCatalogAsync(string platformsPath, string packagesPath, string usagesPath, string catalogModelPath)
    {
        if (File.Exists(catalogModelPath))
            return;

        File.Delete(catalogModelPath);

        var builder = new CatalogBuilder();
        builder.Index(platformsPath);
        builder.Index(packagesPath);

        var usageFiles = GetUsageFiles(usagesPath);
        foreach (var (path, name, date) in usageFiles)
            builder.IndexUsages(path, name, date);

        builder.Build(catalogModelPath);

        var model = await ApiCatalogModel.LoadAsync(catalogModelPath);
        var stats = model.GetStatistics().ToString();
        Console.WriteLine("Catalog stats:");
        Console.Write(stats);
        await File.WriteAllTextAsync(Path.ChangeExtension(catalogModelPath, ".txt"), stats);
    }

    private static async Task GenerateSuffixTreeAsync(string catalogModelPath, string suffixTreePath)
    {
        if (File.Exists(suffixTreePath))
            return;

        Console.WriteLine($"Generating {Path.GetFileName(suffixTreePath)}...");
        var catalog = await ApiCatalogModel.LoadAsync(catalogModelPath);
        var builder = new SuffixTreeBuilder();

        foreach (var api in catalog.AllApis)
        {
            if (api.Kind.IsAccessor())
                continue;

            builder.Add(api.ToString(), api.Id);
        }

        await using var stream = File.Create(suffixTreePath);
        builder.WriteSuffixTree(stream);
    }

    private static async Task UploadCatalogModelAsync(string catalogModelPath)
    {
        Console.WriteLine("Uploading catalog model...");
        var connectionString = GetAzureStorageConnectionString();
        var container = "catalog";
        var name = Path.GetFileName(catalogModelPath);
        var blobClient = new BlobClient(connectionString, container, name, options: GetBlobOptions());
        await blobClient.UploadAsync(catalogModelPath, overwrite: true);
    }

    private static async Task UploadSuffixTreeAsync(string suffixTreePath)
    {
        var compressedFileName = suffixTreePath + ".deflate";
        using (var inputStream = File.OpenRead(suffixTreePath))
        using (var outputStream = File.Create(compressedFileName))
        using (var deflateStream = new DeflateStream(outputStream, CompressionLevel.Optimal))
            await inputStream.CopyToAsync(deflateStream);

        Console.WriteLine("Uploading suffix tree...");
        var connectionString = GetAzureStorageConnectionString();
        var container = "catalog";
        var blobClient = new BlobClient(connectionString, container, "suffixtree.dat.deflate", options: GetBlobOptions());
        await blobClient.UploadAsync(compressedFileName, overwrite: true);
    }

    private static async Task PostToGenCatalogWebHook()
    {
        Console.WriteLine("Invoking webhook...");
        var secrets = Secrets.Load();

        var url = Environment.GetEnvironmentVariable("GenCatalogWebHookUrl");
        if (string.IsNullOrEmpty(url))
            url = secrets?.GenCatalogWebHookUrl;

        var secret = Environment.GetEnvironmentVariable("GenCatalogWebHookSecret");
        if (string.IsNullOrEmpty(secret))
            secret = secrets?.GenCatalogWebHookSecret;

        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(secret))
        {
            Console.WriteLine("warning: cannot retrieve secret for GenCatalog web hook.");
            return;
        }

        try
        {
            var client = new HttpClient();
            var response = await client.PostAsync(url, new StringContent(secret));
            Console.WriteLine($"Webhook returned: {response.StatusCode}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"warning: there was a problem calling the web hook: {ex}");
        }
    }

    private static async Task UploadSummaryAsync(bool success)
    {
        var job = new Job
        {
            Date = DateTimeOffset.UtcNow,
            Success = success,
            DetailsUrl = GetDetailsUrl()
        };

        using var jobStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(jobStream, job);
        jobStream.Position = 0;

        var connectionString = GetAzureStorageConnectionString();
        var container = "catalog";
        var blobClient = new BlobClient(connectionString, container, "job.json", options: GetBlobOptions());
        await blobClient.UploadAsync(jobStream, overwrite: true);
    }

    private static BlobClientOptions GetBlobOptions()
    {
        return new BlobClientOptions
        {
            Retry =
            {
                Mode = RetryMode.Exponential,
                Delay = TimeSpan.FromSeconds(90),
                MaxRetries = 10,
                NetworkTimeout = TimeSpan.FromMinutes(5),
            }
        };
    }

    private static IReadOnlyList<UsageFile> GetUsageFiles(string usagePath)
    {
        var result = new List<UsageFile>();
        var files = Directory.GetFiles(usagePath, "*.tsv");

        foreach (var file in files.OrderBy(f => f))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var date = DateOnly.FromDateTime(File.GetLastWriteTimeUtc(file));
            var usageFile = new UsageFile(file, name, date);
            result.Add(usageFile);
        }

        return result.ToArray();
    }
}

internal sealed class Secrets
{
    public string? AzureStorageConnectionString { get; set; }
    public string? GenCatalogWebHookUrl { get; set; }
    public string? GenCatalogWebHookSecret { get; set; }

    public static Secrets? Load()
    {
        var secretsPath = PathHelper.GetSecretsPathFromSecretsId("ApiCatalog");
        if (!File.Exists(secretsPath))
            return null;

        var secretsJson = File.ReadAllText(secretsPath);
        return JsonSerializer.Deserialize<Secrets>(secretsJson)!;
    }
}

internal sealed class Job
{
    public DateTimeOffset Date { get; set; }
    public bool Success { get; set; }
    public string? DetailsUrl { get; set; }
}

internal record UsageFile(string Path, string Name, DateOnly Date);