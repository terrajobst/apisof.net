//#define USE_CCI

#if !DEBUG
#define PARALLELIZE
#endif

using Arroyo;
using Microsoft.Cci.Extensions;

using NuGet.Frameworks;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

#if PARALLELIZE
using System.Threading.Tasks;
#endif

using Terrajobst.UsageCrawling;

namespace NetUpgradePlanner.Analysis;

internal sealed class AssemblySet
{
    public static AssemblySet Empty { get; } = new AssemblySet(Array.Empty<AssemblySetEntry>());

    public AssemblySet(IEnumerable<AssemblySetEntry> entries)
    {
        Entries = entries.ToArray();
    }

    public bool IsEmpty => Entries.Count == 0;

    public IReadOnlyList<AssemblySetEntry> Entries { get; }

    public AssemblySet Merge(AssemblySet other)
    {
        var mergedEntryByName = new SortedDictionary<string, AssemblySetEntry>();

        foreach (var myEntry in Entries)
            mergedEntryByName.Add(myEntry.Name, myEntry);

        foreach (var otherEntry in other.Entries)
            mergedEntryByName.TryAdd(otherEntry.Name, otherEntry);

        var mergedEntries = mergedEntryByName.Keys.Select(k => mergedEntryByName[k]);
        return new AssemblySet(mergedEntries);
    }

    public AssemblySet Remove(IEnumerable<AssemblySetEntry> entries)
    {
        var newEntries = Entries.ToList();

        foreach (var e in entries)
            newEntries.Remove(e);

        if (newEntries.Count == Entries.Count)
            return this;

        return new AssemblySet(newEntries);
    }

    public static AssemblySet Create(IEnumerable<string> paths)
    {
        return Create(paths, IProgressMonitor.Empty);
    }

    public static AssemblySet Create(IEnumerable<string> paths, IProgressMonitor progressMonitor)
    {
#if USE_CCI
        var materializedPaths = paths.ToArray();

        var entries = new List<AssemblySetEntry>();
        var processedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < materializedPaths.Length; i++)
        {
            progressMonitor.Report(i + 1, materializedPaths.Length);

            var path = materializedPaths[i];

            using var env = new HostEnvironment();
            var assembly = env.LoadAssemblyFrom(path);
            if (assembly is null)
                continue;

            var tfm = assembly.GetTargetFrameworkMoniker();
            var assemblyFramework = string.IsNullOrEmpty(tfm) ? null : NuGetFramework.Parse(tfm).GetShortFolderName();

            var assemblyNames = assembly.Name.Value;
            if (!processedNames.Add(assemblyNames))
                continue;

            var crawler = new AssemblyCrawler();
            crawler.Crawl(assembly);

            var crawlerResults = crawler.GetResults();

            var dependencies = assembly.AssemblyReferences.Select(ar => ar.Name.Value);
            var usedApis = crawlerResults.Data.Select(kv => kv.Key.Guid);
            var entry = new AssemblySetEntry(assemblyNames, assemblyFramework, dependencies, usedApis);
            entries.Add(entry);

            if (entries.Count > 50)
                break;
        }

        return new AssemblySet(entries);
#else
        var materializedPaths = paths.ToArray();

        var assemblyNames = new ConcurrentDictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var entries = new ConcurrentBag<AssemblySetEntry>();

        var current = 0;

#if PARALLELIZE
        Parallel.ForEach(materializedPaths, path =>
#else
        foreach (var path in materializedPaths)
#endif
        {       
            var file = MetadataFile.Open(path);
            if (file is not null && assemblyNames.TryAdd(file.Name, null))
            {
                using var module = file is MetadataAssembly a ? a.MainModule : (MetadataModule)file;
                var tfm = module.ContainingAssembly?.GetTargetFrameworkMoniker();
                var assemblyFramework = string.IsNullOrEmpty(tfm) ? null : NuGetFramework.Parse(tfm).GetShortFolderName();

                var crawler = new AssemblyCrawlerArroyo();
                crawler.Crawl(file);

                var crawlerResults = crawler.GetResults();

                var dependencies = module.AssemblyReferences.Select(ar => ar.Name);
                var usedApis = crawlerResults.Data.Select(kv => kv.Key.Guid);
                var entry = new AssemblySetEntry(file.Name, assemblyFramework, dependencies, usedApis);

                entries.Add(entry);

            }

            Interlocked.Increment(ref current);
            progressMonitor.Report(current, materializedPaths.Length);
        }
#if PARALLELIZE
        );
#endif
        
        return new AssemblySet(entries.OrderBy(e => e.Name));
#endif
    }

    public IEnumerable<AssemblySetEntry> GetAncestors(AssemblySetEntry entry)
    {
        var ancestorsByName = new Dictionary<string, HashSet<AssemblySetEntry>>();

        foreach (var ancestor in Entries)
        {
            foreach (var descendent in ancestor.Dependencies)
            {
                if (!ancestorsByName.TryGetValue(descendent, out var ancestors))
                {
                    ancestors = new HashSet<AssemblySetEntry>();
                    ancestorsByName.Add(descendent, ancestors);
                }

                ancestors.Add(ancestor);
            }
        }

        var result = new HashSet<AssemblySetEntry>();
        var stack = new Stack<AssemblySetEntry>();
        stack.Push(entry);

        while (stack.Count > 0)
        {
            var e = stack.Pop();

            if (ancestorsByName.TryGetValue(e.Name, out var ancestors))
            {
                foreach (var ancestor in ancestors)
                {
                    if (result.Add(ancestor))
                        stack.Push(ancestor);
                }
            }
        }

        return result;
    }

    public IEnumerable<AssemblySetEntry> GetDescendents(AssemblySetEntry entry)
    {
        var entryByName = Entries.ToDictionary(e => e.Name);

        var result = new HashSet<AssemblySetEntry>();
        var stack = new Stack<AssemblySetEntry>();
        stack.Push(entry);

        while (stack.Count > 0)
        {
            var e = stack.Pop();

            foreach (var descendentName in e.Dependencies)
            {
                if (entryByName.TryGetValue(descendentName, out var descendent))
                {
                    if (result.Add(descendent))
                        stack.Push(descendent);
                }
            }
        }

        return result;
    }

    public HashSet<AssemblySetEntry> Butterfly(AssemblySetEntry entry)
    {
        var result = new HashSet<AssemblySetEntry>();
        var ancestors = GetAncestors(entry);
        var descendents = GetDescendents(entry);

        result.UnionWith(ancestors);
        result.Add(entry);
        result.UnionWith(descendents);

        return result;
    }
}
