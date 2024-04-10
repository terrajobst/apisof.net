using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace Terrajobst.ApiCatalog;

public sealed partial class CatalogBuilder
{
    public sealed class CatalogWriter
    {
        private readonly CatalogBuilder _builder;

        private readonly BlobHeap _blobHeap = new();
        private readonly StringHeap _stringHeap = new();
        private readonly PlatformTable _platformTable = new();
        private readonly FrameworkTable _frameworkTable = new();
        private readonly PackageTable _packageTable = new();
        private readonly AssemblyTable _assemblyTable = new();
        private readonly UsageSourceTable _usageSourceTable = new();
        private readonly ApiTable _apiTable = new();
        private readonly RootApiTable _rootApiTable = new();
        private readonly ExtensionMethodTable _extensionMethodTable = new();
        private readonly ObsoletionTable _obsoletionTable = new();
        private readonly PlatformSupportTable _platformSupportTable = new();
        private readonly PreviewRequirementTable _previewRequirementTable = new();
        private readonly ExperimentalTable _experimentalTable = new();

        private readonly Dictionary<IntermediateFramework, FrameworkHandle> _frameworkHandles = new();
        private readonly Dictionary<IntermediatePackage, PackageHandle> _packageHandles = new();
        private readonly Dictionary<IntermediaAssembly, AssemblyHandle> _assemblyHandles = new();
        private readonly Dictionary<IntermediateUsageSource, UsageSourceHandle> _usageSourceHandles = new();
        private readonly Dictionary<string, BlobHandle> _syntaxHandles = new();
        private readonly Dictionary<IntermediaApi, ApiHandle> _apiHandles = new();

        public CatalogWriter(CatalogBuilder builder)
        {
            ThrowIfNull(builder);
            
            _builder = builder;
        }

        public void Write(Stream stream)
        {
            var heapsAndTables = new HeapOrTable[] {
                _stringHeap,
                _blobHeap,
                _platformTable,
                _frameworkTable,
                _packageTable,
                _assemblyTable,
                _usageSourceTable,
                _apiTable,
                _rootApiTable,
                _extensionMethodTable,
                _obsoletionTable,
                _platformSupportTable,
                _previewRequirementTable,
                _experimentalTable
            };

            EnterFrameworkHandles();
            EnterPackageHandles();
            EnterAssemblyHandles();
            EnterUsageSourceHandles();
            EnterApiHandles();
            
            WritePlatforms();
            WriteFrameworks();
            WritePackages();
            WriteAssemblies();
            WriteUsageSources();
            WriteSyntax();
            WriteApis();
            WriteRootApis();
            WriteExtensionMethods();
            WriteObsoletions();
            WritePlatformSupports();
            WritePreviewRequirements();
            WriteExperimentals();

            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                // Magic value

                writer.Write(ApiCatalogSchema.MagicNumber);

                // Version

                writer.Write(ApiCatalogSchema.FormatVersion);

                // Table Sizes
                writer.Write(heapsAndTables.Length);

                foreach (var heapOrTable in heapsAndTables)
                {
                    var length = heapOrTable.Memory.GetLength();
                    writer.Write(length);
                }
            }

            using (var deflateStream = new DeflateStream(stream, CompressionLevel.Optimal, leaveOpen: true))
            {
                foreach (var heapOrTable in heapsAndTables)
                    heapOrTable.Memory.CopyTo(deflateStream);
            }
        }

        private void EnterFrameworkHandles()
        {
            foreach (var framework in _builder._frameworkByName.Values)
            {
                var handle = new FrameworkHandle(_frameworkHandles.Count);
                _frameworkHandles.Add(framework, handle);
            }
        }

        private void EnterPackageHandles()
        {
            foreach (var package in _builder._packageByFingerprint.Values)
            {
                var handle = new PackageHandle(_packageHandles.Count);
                _packageHandles.Add(package, handle);
            }
        }

        private void EnterAssemblyHandles()
        {
            foreach (var assembly in _builder._assemblies)
            {
                var handle = new AssemblyHandle(_assemblyHandles.Count);
                _assemblyHandles.Add(assembly, handle);
            }
        }

        private void EnterUsageSourceHandles()
        {
            foreach (var usageSource in _builder._usageSources.Values)
            {
                var handle = new UsageSourceHandle(_usageSourceHandles.Count);
                _usageSourceHandles.Add(usageSource, handle);
            }
        }

        private void EnterApiHandles()
        {
            EnterHandles(_apiHandles, _builder._rootApis);

            static void EnterHandles(Dictionary<IntermediaApi, ApiHandle> handles, IEnumerable<IntermediaApi> apis)
            {
                foreach (var api in apis)
                {
                    var handle = new ApiHandle(handles.Count);
                    handles.Add(api, handle);
                    
                    if (api.Children is not null)
                        EnterHandles(handles, api.Children);
                }
            }
        }

        private static void EnsureEnteredHandleMatches<TValue, THandleTag>(Dictionary<TValue, Handle<THandleTag>> handles, TValue value, Handle<THandleTag> actualHandle)
            where TValue : notnull
        {
            var expectedHandle = handles[value];
            if (expectedHandle != actualHandle)
                throw new Exception($"The entered handle {expectedHandle} doesn't match the actual handle {actualHandle}");
        }

        private void WritePlatforms()
        {
            Console.WriteLine("Writing platforms...");

            var platforms = _builder._platformNames;

            foreach (var platform in platforms)
            {
                var name = _stringHeap.Store(platform);
                _platformTable.WriteRow(name);
            }
        }

        private void WriteFrameworks()
        {
            Console.WriteLine("Writing frameworks...");

            var frameworks = _builder._frameworkByName.Values.ToArray();

            foreach (var framework in frameworks)
            {
                var name = _stringHeap.Store(framework.Name);
                var assemblies = _blobHeap.StoreAssemblies(framework.Assemblies, _assemblyHandles);

                var handle = _frameworkTable.WriteRow(name, assemblies);
                EnsureEnteredHandleMatches(_frameworkHandles, framework, handle); 
            }
        }

        private void WritePackages()
        {
            Console.WriteLine("Writing packages...");

            var packages = _builder._packageByFingerprint.Values.ToArray();

            foreach (var package in packages)
            {
                var name = _stringHeap.Store(package.Name);
                var version = _stringHeap.Store(package.Version);
                var assemblies = _blobHeap.StoreAssemblies(package.Assemblies, _frameworkHandles, _assemblyHandles);

                var handle = _packageTable.WriteRow(name, version, assemblies);
                EnsureEnteredHandleMatches(_packageHandles, package, handle); 
            }
        }

        private void WriteAssemblies()
        {
            Console.WriteLine("Writing assemblies...");

            var assemblies = _builder._assemblies;

            foreach (var assembly in assemblies)
            {
                var fingerprint = assembly.Fingerprint;
                var name = _stringHeap.Store(assembly.Name);
                var publicKeyToken = _stringHeap.Store(assembly.PublicKeyToken);
                var version = _stringHeap.Store(assembly.Version);
                var rootApis = _blobHeap.StoreApis(assembly.RootApis.ToArray(), _apiHandles);
                var frameworks = _blobHeap.StoreFrameworks(assembly.Frameworks, _frameworkHandles);
                var packages = _blobHeap.StorePackages(assembly.Packages, _packageHandles, _frameworkHandles);

                var handle = _assemblyTable.WriteRow(fingerprint, name, publicKeyToken, version, rootApis, frameworks, packages);
                EnsureEnteredHandleMatches(_assemblyHandles, assembly, handle);
            }
        }

        private void WriteUsageSources()
        {
            Console.WriteLine("Writing data sources...");

            var usageSources = _builder._usageSources.Values;

            foreach (var usageSource in usageSources)
            {
                var name = _stringHeap.Store(usageSource.Name);
                var dayNumber = usageSource.Date.DayNumber;

                var handle = _usageSourceTable.WriteRow(name, dayNumber);
                EnsureEnteredHandleMatches(_usageSourceHandles, usageSource, handle);
            }
        }

        private void WriteSyntax()
        {
            Console.WriteLine("Writing syntax...");

            foreach (var assembly in _builder._assemblies)
            {
                foreach (var declaration in assembly.Declarations.Values)
                {
                    ref var handle = ref CollectionsMarshal.GetValueRefOrAddDefault(_syntaxHandles, declaration.Syntax, out var exists);
                    if (!exists)
                        handle = _blobHeap.StoreSyntax(declaration.Syntax, _stringHeap, _builder._apiByFingerprint, _apiHandles);
                }
            }
        }
        
        private void WriteApis()
        {
            Console.WriteLine("Writing APIs...");

            WriteApis(_builder._rootApis);
        }

        private void WriteApis(IReadOnlyList<IntermediaApi> apis)
        {
            foreach (var api in apis)
            {
                var intermediateChildren = (IReadOnlyList<IntermediaApi>?) api.Children ?? Array.Empty<IntermediaApi>();
                var intermediateDeclarations = GetDeclarations(_builder, api);
                var intermediateUsages = GetUsages(_builder, api);

                var fingerprint = api.Fingerprint;
                var kind = (byte)api.Kind;
                var parent = api.Parent is null ? ApiHandle.Nil : _apiHandles[api.Parent]; // NOTE: This is safe because we know the parent was already written.
                var name = _stringHeap.Store(api.Name);
                var children = _blobHeap.StoreApis(intermediateChildren, _apiHandles);
                var declarations = _blobHeap.StoreDeclarations(intermediateDeclarations, _assemblyHandles, _syntaxHandles);
                var usages = _blobHeap.StoreUsages(intermediateUsages, _usageSourceHandles);

                var handle = _apiTable.WriteRow(fingerprint, kind, parent, name, children, declarations, usages);
                EnsureEnteredHandleMatches(_apiHandles, api, handle);

                if (api.Children is not null)
                    WriteApis(api.Children);
            }

            static IReadOnlyList<IntermediateDeclaration> GetDeclarations(CatalogBuilder builder, IntermediaApi api)
            {
                var result = new List<IntermediateDeclaration>();

                foreach (var assembly in builder._assemblies)
                {
                    if (assembly.Declarations.TryGetValue(api, out var declaration))
                        result.Add(declaration);
                }

                return result;
            }

            static IReadOnlyList<(IntermediateUsageSource UsageSource, float Percentage)> GetUsages(CatalogBuilder builder, IntermediaApi api)
            {
                var result = new List<(IntermediateUsageSource, float)>();

                foreach (var usageSource in builder._usageSources.Values)
                {
                    if (usageSource.Usages.TryGetValue(api.Fingerprint, out var percentage))
                        result.Add((usageSource, percentage));
                }

                return result;
            }
        }

        private void WriteRootApis()
        {
            Console.WriteLine("Writing root APIs...");

            foreach (var entry in _builder._rootApis)
            {
                var api = _apiHandles[entry];
                _rootApiTable.WriteRow(api);
            }
        }

        private void WriteExtensionMethods()
        {
            Console.WriteLine("Writing existing methods...");

            var entries = _builder._apis
                .Where(a => a.Extensions is not null)
                .SelectMany(type => type.Extensions!, (type, extension) => (
                    ExtensionMethodGuid: extension.Fingerprint,
                    ExtendedType: _apiHandles[type],
                    ExtensionMethod: _apiHandles[extension.Method]))
                .OrderBy(t => t.ExtendedType.Value)
                .ThenBy(t => t.ExtensionMethod.Value)
                .ToArray();

            foreach (var entry in entries)
            {
                var extensionMethodGuid = entry.ExtensionMethodGuid;
                var extendedType = entry.ExtendedType;
                var extensionMethod = entry.ExtensionMethod;
                _extensionMethodTable.WriteRow(extensionMethodGuid, extendedType, extensionMethod);
            }
        }

        private void WriteObsoletions()
        {
            Console.WriteLine("Writing obsoletions...");

            var entries = _builder._assemblies
                .SelectMany(a => a.Declarations.Values)
                .Where(d => d.Obsoletion is not null)
                .Select(d => (
                    Api: _apiHandles[d.Api],
                    Assembly: _assemblyHandles[d.Assembly],
                    Message: _stringHeap.Store(d.Obsoletion!.Message),
                    d.Obsoletion.IsError,
                    DiagnosticId: _stringHeap.Store(d.Obsoletion.DiagnosticId),
                    UrlFormat: _stringHeap.Store(d.Obsoletion.UrlFormat)
                ))
                .OrderBy(t => t.Api.Value)
                .ThenBy(t => t.Assembly.Value)
                .ToArray();

            foreach (var entry in entries)
            {
                var api = entry.Api;
                var assembly = entry.Assembly;
                var message = entry.Message;
                var isError = entry.IsError;
                var diagnosticId = entry.DiagnosticId;
                var urlFormat = entry.UrlFormat;

                _obsoletionTable.WriteRow(api, assembly, message, isError, diagnosticId, urlFormat);
            }
        }

        private void WritePlatformSupports()
        {
            Console.WriteLine("Writing platform support...");

            var assemblyPlatformSupport = _builder._assemblies
                .Select(a => (Assembly: a, Api: (IntermediaApi?)null, a.PlatformSupport));

            var apiPlatformSupport = _builder._assemblies
                .SelectMany(a => a.Declarations, (a, d) => (Assembly: a, Api: (IntermediaApi?)d.Key, d.Value.PlatformSupport));

            var entries = assemblyPlatformSupport
                .Concat(apiPlatformSupport)
                .Where(e => e.PlatformSupport is not null)
                .Select(e => (
                    Api: e.Api is null ? ApiHandle.Nil : _apiHandles[e.Api],
                    Assembly: _assemblyHandles[e.Assembly],
                    Platforms: _blobHeap.StorePlatformSupport(e.PlatformSupport!, _stringHeap)
                ))
                .OrderBy(t => t.Api.Value)
                .ThenBy(t => t.Assembly.Value)
                .ToArray();

            foreach (var entry in entries)
            {
                var api = entry.Api;
                var assembly = entry.Assembly;
                var platforms = entry.Platforms;
                _platformSupportTable.WriteRow(api, assembly, platforms);
            }
        }

        private void WritePreviewRequirements()
        {
            Console.WriteLine("Writing preview requirements...");

            var assemblyPreviewRequirements = _builder._assemblies
                .Select(a => (Assembly: a, Api: (IntermediaApi?)null, a.PreviewRequirement));

            var apiPreviewRequirements = _builder._assemblies
                .SelectMany(a => a.Declarations, (a, d) => (Assembly: a, Api: (IntermediaApi?)d.Key, d.Value.PreviewRequirement));

            var entries = assemblyPreviewRequirements
                .Concat(apiPreviewRequirements)
                .Where(e => e.PreviewRequirement is not null)
                .Select(e => (
                    Api: e.Api is null ? ApiHandle.Nil : _apiHandles[e.Api],
                    Assembly: _assemblyHandles[e.Assembly],
                    Message: _stringHeap.Store(e.PreviewRequirement!.Message),
                    Url: _stringHeap.Store(e.PreviewRequirement.Url)
                ))
                .OrderBy(t => t.Api.Value)
                .ThenBy(t => t.Assembly.Value)
                .ToArray();

            foreach (var entry in entries)
            {
                var api = entry.Api;
                var assembly = entry.Assembly;
                var message = entry.Message;
                var url = entry.Url;

                _previewRequirementTable.WriteRow(api, assembly, message, url);
            }
        }

        private void WriteExperimentals()
        {
            Console.WriteLine("Writing experimentals...");

            var assemblyExperimental = _builder._assemblies
                .Select(a => (Assembly: a, Api: (IntermediaApi?)null, a.Experimental));

            var apiExperimental = _builder._assemblies
                .SelectMany(a => a.Declarations, (a, d) => (Assembly: a, Api: (IntermediaApi?)d.Key, d.Value.Experimental));

            var entries = assemblyExperimental
                .Concat(apiExperimental)
                .Where(e => e.Experimental is not null)
                .Select(e => (
                    Api: e.Api is null ? ApiHandle.Nil : _apiHandles[e.Api],
                    Assembly: _assemblyHandles[e.Assembly],
                    DiagnosticId: _stringHeap.Store(e.Experimental!.DiagnosticId),
                    UrlFormat: _stringHeap.Store(e.Experimental.UrlFormat)
                ))
                .OrderBy(t => t.Api.Value)
                .ThenBy(t => t.Assembly.Value)
                .ToArray();

            foreach (var entry in entries)
            {
                var api = entry.Api;
                var assembly = entry.Assembly;
                var diagnosticId = entry.DiagnosticId;
                var urlFormat = entry.UrlFormat;

                _experimentalTable.WriteRow(api, assembly, diagnosticId, urlFormat);
            }
        }

        private class Memory
        {
            private readonly MemoryStream _data = new();
            private readonly BinaryWriter _writer;

            public Memory()
            {
                _writer = new BinaryWriter(_data, Encoding.UTF8, leaveOpen: true);
            }

            public void Clear()
            {
                _writer.Flush();
                _data.SetLength(0);
                Debug.Assert(_data.Position == 0);
            }

            public int GetLength()
            {
                _writer.Flush();
                return (int)_data.Length;
            }

            public ArraySegment<byte> GetData()
            {
                _writer.Flush();
                var success = _data.TryGetBuffer(out var buffer);
                Debug.Assert(success);
                return buffer;
            }

            public void CopyTo(Stream destination)
            {
                _writer.Flush();
                _data.Position = 0;
                _data.CopyTo(destination);
            }

            public void WriteStringHandle(StringHandle handle)
            {
                WriteInt32(handle.Value);
            }

            public void WriteBlobHandle(BlobHandle handle)
            {
                WriteInt32(handle.Value);
            }

            public void WriteFrameworkHandle(FrameworkHandle handle)
            {
                WriteInt32(handle.Value);
            }

            public void WritePackageHandle(PackageHandle handle)
            {
                WriteInt32(handle.Value);
            }

            public void WriteAssemblyHandle(AssemblyHandle handle)
            {
                WriteInt32(handle.Value);
            }

            public void WriteUsageSourceHandle(UsageSourceHandle handle)
            {
                WriteInt32(handle.Value);
            }

            public void WriteApiHandle(ApiHandle handle)
            {
                WriteInt32(handle.Value);
            }

            public void WriteString(string value)
            {
                // NOTE: We don't do _writer.Write(value) because that writes a 7-bit encoded int and I'm too lazy to
                //       implement that on the reader side.
                //
                // Doing so would shave off ~3 MB.

                var length = value.Length;
                var bytes = Encoding.UTF8.GetBytes(value);
                WriteInt32(length);
                _writer.Write(bytes);
            }

            public void WriteBool(bool value)
            {
                var b = value ? (byte)1 : (byte)0;
                WriteByte(b);
            }

            public void WriteByte(byte value)
            {
                _writer.Write(value);
            }

            public void WriteGuid(Guid guid)
            {
                var bytes = guid.ToByteArray();
                WriteBytes(bytes);
            }

            public void WriteInt32(int value)
            {
                _writer.Write(value);
            }

            public void WriteSingle(float value)
            {
                _writer.Write(value);
            }

            public void WriteBytes(ReadOnlySpan<byte> bytes)
            {
                _writer.Write(bytes);
            }
        }

        private abstract class HeapOrTable
        {
            public Memory Memory { get; } = new();
        }

        private sealed class DeduplicatedMemory : Memory
        {
            private readonly Memory _underlyingMemory;
            private readonly Dictionary<int, (int Start, int Length)> _existingBlobs = new();

            public DeduplicatedMemory(Memory underlyingMemory)
            {
                _underlyingMemory = underlyingMemory;
            }

            public BlobHandle Commit()
            {
                var data = GetData();
                var offset = _underlyingMemory.GetLength();

                (var added, offset) = AddBlob(offset, data);
                if (added)
                    _underlyingMemory.WriteBytes(data);

                Clear();
                return new BlobHandle(offset);
            }

            // This algorithm is taken from System.Reflection.Metadata.
            //
            // The idea is to use an int-based hash for blobs and do double hashing to resolve conflicts.

            private (bool Added, int Offset) AddBlob(int offset, ArraySegment<byte> newBlob)
            {
                var dictionaryKey = Hash.GetFNVHashCode(newBlob);
                while (true)
                {
                    // First lets see whether we can find the bucket for the hash

                    if (!_existingBlobs.TryGetValue(dictionaryKey, out var entry))
                    {
                        // No value for that key. That means the blob wasn't added yet.
                        entry = (offset, newBlob.Count);
                        _existingBlobs.Add(dictionaryKey, entry);
                        return (true, offset);
                    }

                    // We found an for the key. However it could be taken by another blob.
                    // Let's compare contents:

                    var existingBlob = _underlyingMemory.GetData().Slice(entry.Start, entry.Length);
                    if (existingBlob.SequenceEqual(newBlob))
                    {
                        // We have seen the blob. Nice!
                        return (false, entry.Start);
                    }

                    // We found the entry for a different blob. Keep looking.

                    dictionaryKey = GetNextDictionaryKey(dictionaryKey);
                }
            }

            private static int GetNextDictionaryKey(int dictionaryKey) => (int)((uint)dictionaryKey * 747796405 + 2891336453);

            internal static class Hash
            {
                private const int FnvOffsetBias = unchecked((int)2166136261);
                private const int FnvPrime = 16777619;

                public static int GetFNVHashCode(ReadOnlySpan<byte> data)
                {
                    var hashCode = FnvOffsetBias;

                    for (var i = 0; i < data.Length; i++)
                        hashCode = unchecked((hashCode ^ data[i]) * FnvPrime);

                    return hashCode;
                }
            }
        }

        private sealed class BlobHeap : HeapOrTable
        {
            public BlobHeap()
            {
                DeduplicatedMemory = new DeduplicatedMemory(Memory);
            }

            private DeduplicatedMemory DeduplicatedMemory { get; }

            private BlobHandle GetEnd()
            {
                var end = Memory.GetLength();
                return new BlobHandle(end);
            }

            public BlobHandle StoreAssemblies(IReadOnlyList<IntermediaAssembly> assemblies,
                                              IReadOnlyDictionary<IntermediaAssembly, AssemblyHandle> assemblyHandles)
            {
                if (assemblies.Count == 0)
                    return BlobHandle.Nil;

                var result = GetEnd();

                Memory.WriteInt32(assemblies.Count);
                foreach (var assembly in assemblies)
                {
                    var handle = assemblyHandles[assembly];
                    Memory.WriteAssemblyHandle(handle);
                }

                return result;
            }

            public BlobHandle StoreAssemblies(IReadOnlyList<(IntermediateFramework, IntermediaAssembly)> assemblies,
                                              IReadOnlyDictionary<IntermediateFramework, FrameworkHandle> frameworkHandles,
                                              IReadOnlyDictionary<IntermediaAssembly, AssemblyHandle> assemblyHandles)
            {
                if (assemblies.Count == 0)
                    return BlobHandle.Nil;

                var result = GetEnd();

                Memory.WriteInt32(assemblies.Count);
                foreach (var (framework, assembly) in assemblies)
                {
                    Memory.WriteFrameworkHandle(frameworkHandles[framework]);
                    Memory.WriteAssemblyHandle(assemblyHandles[assembly]);
                }

                return result;
            }

            public BlobHandle StoreFrameworks(IReadOnlyList<IntermediateFramework> frameworks,
                                              IReadOnlyDictionary<IntermediateFramework, FrameworkHandle> frameworkHandles)
            {
                if (frameworks.Count == 0)
                    return BlobHandle.Nil;

                var result = GetEnd();

                Memory.WriteInt32(frameworks.Count);
                foreach (var framework in frameworks)
                    Memory.WriteFrameworkHandle(frameworkHandles[framework]);

                return result;
            }

            public BlobHandle StorePackages(IReadOnlyList<(IntermediatePackage, IntermediateFramework)> packages,
                                            IReadOnlyDictionary<IntermediatePackage, PackageHandle> packageHandles,
                                            IReadOnlyDictionary<IntermediateFramework, FrameworkHandle> frameworkHandles)
            {
                if (packages.Count == 0)
                    return BlobHandle.Nil;

                var result = GetEnd();

                Memory.WriteInt32(packages.Count);
                foreach (var (package, framework) in packages)
                {
                    Memory.WritePackageHandle(packageHandles[package]);
                    Memory.WriteFrameworkHandle(frameworkHandles[framework]);
                }

                return result;
            }

            public BlobHandle StoreApis(IReadOnlyList<IntermediaApi> apis,
                                        IReadOnlyDictionary<IntermediaApi, ApiHandle> apiHandles)
            {
                if (apis.Count == 0)
                    return BlobHandle.Nil;

                var result = GetEnd();

                Memory.WriteInt32(apis.Count);
                foreach (var api in apis)
                {
                    var handle = apiHandles[api];
                    Memory.WriteApiHandle(handle);
                }

                return result;
            }

            public BlobHandle StoreDeclarations(IReadOnlyList<IntermediateDeclaration> declarations,
                                                IReadOnlyDictionary<IntermediaAssembly, AssemblyHandle> assemblyHandles,
                                                IReadOnlyDictionary<string, BlobHandle> syntaxHandles)
            {
                if (declarations.Count == 0)
                    return BlobHandle.Nil;

                var result = GetEnd();

                Memory.WriteInt32(declarations.Count);
                foreach (var declaration in declarations)
                {
                    var assemblyHandle = assemblyHandles[declaration.Assembly];
                    var syntaxHandle = syntaxHandles[declaration.Syntax];
                    Memory.WriteAssemblyHandle(assemblyHandle);
                    Memory.WriteBlobHandle(syntaxHandle);
                }

                return result;
            }

            public BlobHandle StoreUsages(IReadOnlyList<(IntermediateUsageSource UsageSource, float Percentage)> usages,
                                          IReadOnlyDictionary<IntermediateUsageSource, UsageSourceHandle> usageSourceHandles)
            {
                if (usages.Count == 0)
                    return BlobHandle.Nil;

                var result = GetEnd();

                Memory.WriteInt32(usages.Count);
                foreach (var usage in usages)
                {
                    Memory.WriteUsageSourceHandle(usageSourceHandles[usage.UsageSource]);
                    Memory.WriteSingle(usage.Percentage);
                }

                return result;
            }

            public BlobHandle StorePlatformSupport(IReadOnlyList<IntermediatePlatformSupport> platforms,
                                                   StringHeap stringHeap)
            {
                if (platforms.Count == 0)
                    return BlobHandle.Nil;

                DeduplicatedMemory.WriteInt32(platforms.Count);
                foreach (var platformSupport in platforms
                             .OrderBy(p => p.PlatformName, StringComparer.OrdinalIgnoreCase)
                             .ThenBy(p => p.IsSupported))
                {
                    DeduplicatedMemory.WriteStringHandle(stringHeap.Store(platformSupport.PlatformName));
                    DeduplicatedMemory.WriteBool(platformSupport.IsSupported);
                }

                return DeduplicatedMemory.Commit();
            }

            public BlobHandle StoreSyntax(string syntax,
                                          StringHeap stringHeap,
                                          IReadOnlyDictionary<Guid, IntermediaApi> apiByFingerprint,
                                          IReadOnlyDictionary<IntermediaApi, ApiHandle> apiHandles)
            {
                var markup = Markup.FromXml(syntax);

                var result = GetEnd();
                Memory.WriteInt32(markup.Tokens.Length);

                foreach (var token in markup.Tokens)
                {
                    var kind = (byte)token.Kind;
                    var text = stringHeap.Store(token.Text);
                    var hasIntrinsicText = token.Kind.GetTokenText() is not null;

                    Memory.WriteByte(kind);
                    if (!hasIntrinsicText)
                        Memory.WriteStringHandle(text);

                    if (token.Kind == MarkupTokenKind.ReferenceToken)
                    {
                        if (token.Reference is not null && apiByFingerprint.TryGetValue(token.Reference.Value, out var api))
                            Memory.WriteApiHandle(apiHandles[api]);
                        else
                            Memory.WriteInt32(-1);
                    }
                }

                return result;
            }
        }

        private sealed class StringHeap : HeapOrTable
        {
            private readonly Dictionary<string, StringHandle> _stringHandles = new (StringComparer.Ordinal);

            public StringHandle Store(string text)
            {
                if (!_stringHandles.TryGetValue(text, out var handle))
                {
                    handle = new StringHandle(Memory.GetLength());
                    Memory.WriteString(text);
                    _stringHandles.Add(text, handle);
                }

                return handle;
            }
        }

        private sealed class PlatformTable : HeapOrTable
        {
            public void WriteRow(StringHandle name)
            {
                Memory.WriteStringHandle(name);
            }
        }

        private sealed class FrameworkTable : HeapOrTable
        {
            public FrameworkHandle WriteRow(StringHandle name,
                                            BlobHandle assemblies)
            {
                var handle = new FrameworkHandle(Memory.GetLength() / ApiCatalogSchema.FrameworkRow.Size);

                Memory.WriteStringHandle(name);
                Memory.WriteBlobHandle(assemblies);

                return handle;
            }
        }

        private sealed class PackageTable : HeapOrTable
        {
            public PackageHandle WriteRow(StringHandle packageName,
                                          StringHandle packageVersion,
                                          BlobHandle assemblies)
            {
                var handle = new PackageHandle(Memory.GetLength() / ApiCatalogSchema.PackageRow.Size);

                Memory.WriteStringHandle(packageName);
                Memory.WriteStringHandle(packageVersion);
                Memory.WriteBlobHandle(assemblies);

                return handle;
            }
        }

        private sealed class AssemblyTable : HeapOrTable
        {
            public AssemblyHandle WriteRow(Guid fingerprint,
                                           StringHandle name,
                                           StringHandle publicKeyToken,
                                           StringHandle version,
                                           BlobHandle rootApis,
                                           BlobHandle frameworks,
                                           BlobHandle packages)
            {
                var handle = new AssemblyHandle(Memory.GetLength() / ApiCatalogSchema.AssemblyRow.Size);

                Memory.WriteGuid(fingerprint);
                Memory.WriteStringHandle(name);
                Memory.WriteStringHandle(publicKeyToken);
                Memory.WriteStringHandle(version);
                Memory.WriteBlobHandle(rootApis);
                Memory.WriteBlobHandle(frameworks);
                Memory.WriteBlobHandle(packages);

                return handle;
            }
        }

        private sealed class UsageSourceTable : HeapOrTable
        {
            public UsageSourceHandle WriteRow(StringHandle name,
                                              int dayNumber)
            {
                var handle = new UsageSourceHandle(Memory.GetLength() / ApiCatalogSchema.UsageSourceRow.Size);

                Memory.WriteStringHandle(name);
                Memory.WriteInt32(dayNumber);

                return handle;
            }
        }

        private sealed class ApiTable : HeapOrTable
        {
            public ApiHandle WriteRow(Guid fingerprint,
                                      byte kind,
                                      ApiHandle parent,
                                      StringHandle name,
                                      BlobHandle children,
                                      BlobHandle declarations,
                                      BlobHandle usages)
            {
                var handle = new ApiHandle(Memory.GetLength() / ApiCatalogSchema.ApiRow.Size);

                Memory.WriteGuid(fingerprint);
                Memory.WriteByte(kind);
                Memory.WriteApiHandle(parent);
                Memory.WriteStringHandle(name);
                Memory.WriteBlobHandle(children);
                Memory.WriteBlobHandle(declarations);
                Memory.WriteBlobHandle(usages);

                return handle;
            }
        }

        private sealed class RootApiTable : HeapOrTable
        {
            public void WriteRow(ApiHandle api)
            {
                Memory.WriteApiHandle(api);
            }
        }

        private sealed class ObsoletionTable : HeapOrTable
        {
            public void WriteRow(ApiHandle api,
                                 AssemblyHandle assembly,
                                 StringHandle message,
                                 bool isError,
                                 StringHandle diagnosticId,
                                 StringHandle urlFormat)
            {
                Memory.WriteApiHandle(api);
                Memory.WriteAssemblyHandle(assembly);
                Memory.WriteStringHandle(message);
                Memory.WriteBool(isError);
                Memory.WriteStringHandle(diagnosticId);
                Memory.WriteStringHandle(urlFormat);
            }
        }

        private sealed class PlatformSupportTable : HeapOrTable
        {
            public void WriteRow(ApiHandle api,
                                 AssemblyHandle assembly,
                                 BlobHandle platforms)
            {
                Memory.WriteApiHandle(api);
                Memory.WriteAssemblyHandle(assembly);
                Memory.WriteBlobHandle(platforms);
            }
        }

        private sealed class PreviewRequirementTable : HeapOrTable
        {
            public void WriteRow(ApiHandle api,
                                 AssemblyHandle assembly,
                                 StringHandle message,
                                 StringHandle url)
            {
                Memory.WriteApiHandle(api);
                Memory.WriteAssemblyHandle(assembly);
                Memory.WriteStringHandle(message);
                Memory.WriteStringHandle(url);
            }
        }

        private sealed class ExperimentalTable : HeapOrTable
        {
            public void WriteRow(ApiHandle api,
                                 AssemblyHandle assembly,
                                 StringHandle diagnosticId,
                                 StringHandle urlFormat)
            {
                Memory.WriteApiHandle(api);
                Memory.WriteAssemblyHandle(assembly);
                Memory.WriteStringHandle(diagnosticId);
                Memory.WriteStringHandle(urlFormat);
            }
        }

        private sealed class ExtensionMethodTable : HeapOrTable
        {
            public void WriteRow(Guid extensionMethodGuid,
                                 ApiHandle extendedType,
                                 ApiHandle extensionMethod)
            {
                Memory.WriteGuid(extensionMethodGuid);
                Memory.WriteApiHandle(extendedType);
                Memory.WriteApiHandle(extensionMethod);
            }
        }
    }
}