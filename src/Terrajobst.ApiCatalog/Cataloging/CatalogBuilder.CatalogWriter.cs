﻿#nullable enable

using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
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
        private readonly ObsoletionTable _obsoletionTable = new();
        private readonly PlatformSupportTable _platformSupportTable = new();
        private readonly PreviewRequirementTable _previewRequirementTable = new();
        private readonly ExperimentalTable _experimentalTable = new();
        private readonly ExtensionMethodTable _extensionMethodTable = new();

        private readonly Dictionary<IntermediateFramework, FrameworkOffset> _frameworkOffsets = new();
        private readonly Dictionary<IntermediatePackage, PackageOffset> _packageOffsets = new();
        private readonly Dictionary<IntermediaAssembly, AssemblyOffset> _assemblyOffsets = new();
        private readonly Dictionary<IntermediateUsageSource, UsageSourceOffset> _usageSourceOffsets = new();
        private readonly Dictionary<IntermediaApi, ApiOffset> _apiOffsets = new();

        public CatalogWriter(CatalogBuilder builder)
        {
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
                _obsoletionTable,
                _platformSupportTable,
                _previewRequirementTable,
                _experimentalTable,
                _extensionMethodTable
            };

            WritePlatforms();
            WriteFrameworks();
            WritePackages();
            WriteAssemblies();
            WriteUsageSources();
            WriteApis();
            WriteRootApis();
            WriteObsoletions();
            WritePlatformSupports();
            WritePreviewRequirements();
            WriteExperimentals();
            WriteExtensionMethods();

            _blobHeap.PatchSyntaxes(_builder, _stringHeap);
            _blobHeap.PatchApiOffsets(_builder._apis, _apiOffsets);
            _blobHeap.PatchAssemblyOffsets(_builder._assemblies, _assemblyOffsets);

            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                // Magic value

                writer.Write(ApiCatalogSchema.MagicHeader);

                // Version

                writer.Write(ApiCatalogSchema.Version);

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

        private void WritePlatforms()
        {
            var platforms = _builder._platformNames;

            foreach (var platform in platforms)
            {
                var name = _stringHeap.Store(platform);
                _platformTable.WriteRow(name);
            }
        }

        private void WriteFrameworks()
        {
            var frameworks = _builder._frameworkByName.Values.ToArray();

            foreach (var framework in frameworks)
            {
                var name = _stringHeap.Store(framework.Name);
                var assemblies = _blobHeap.StoreAssemblies(framework.Assemblies);

                var offset = _frameworkTable.WriteRow(name, assemblies);
                _frameworkOffsets.Add(framework, offset);
            }
        }

        private void WritePackages()
        {
            var packages = _builder._packageByFingerprint.Values.ToArray();

            foreach (var package in packages)
            {
                var name = _stringHeap.Store(package.Name);
                var version = _stringHeap.Store(package.Version);
                var assemblies = _blobHeap.StoreAssemblies(package.Assemblies, _frameworkOffsets);

                var rowOffset = _packageTable.WriteRow(name, version, assemblies);
                _packageOffsets.Add(package, rowOffset);
            }
        }

        private void WriteAssemblies()
        {
            var assemblies = _builder._assemblies;

            foreach (var assembly in assemblies)
            {
                var fingerprint = assembly.Fingerprint;
                var name = _stringHeap.Store(assembly.Name);
                var publicKeyToken = _stringHeap.Store(assembly.PublicKeyToken);
                var version = _stringHeap.Store(assembly.Version);
                var rootApis = _blobHeap.StoreApis(assembly.RootApis.ToArray());
                var frameworks = _blobHeap.StoreFrameworks(assembly.Frameworks, _frameworkOffsets);
                var packages = _blobHeap.StorePackages(assembly.Packages, _packageOffsets, _frameworkOffsets);

                var rowOffset = _assemblyTable.WriteRow(fingerprint, name, publicKeyToken, version, rootApis, frameworks, packages);
                _assemblyOffsets.Add(assembly, rowOffset);
            }
        }

        private void WriteUsageSources()
        {
            var usageSources = _builder._usageSources.Values;

            foreach (var usageSource in usageSources)
            {
                var name = _stringHeap.Store(usageSource.Name);
                var dayNumber = usageSource.Date.DayNumber;

                var rowOffset = _usageSourceTable.WriteRow(name, dayNumber);
                _usageSourceOffsets.Add(usageSource, rowOffset);
            }
        }

        private void WriteApis()
        {
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
                var parent = api.Parent is null ? ApiOffset.Nil : _apiOffsets[api.Parent]; // NOTE: This is safe because we know the parent was already written.
                var name = _stringHeap.Store(api.Name);
                var children = _blobHeap.StoreApis(intermediateChildren);
                var declarations = _blobHeap.StoreDeclarations(intermediateDeclarations, _assemblyOffsets);
                var usages = _blobHeap.StoreUsages(intermediateUsages, _usageSourceOffsets);

                var rowOffset = _apiTable.WriteRow(fingerprint, kind, parent, name, children, declarations, usages);
                _apiOffsets.Add(api, rowOffset);

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
            foreach (var entry in _builder._rootApis)
            {
                var api = _apiOffsets[entry];
                _rootApiTable.WriteRow(api);
            }
        }

        private void WriteObsoletions()
        {
            var entries = _builder._assemblies
                .SelectMany(a => a.Declarations.Values)
                .Where(d => d.Obsoletion is not null)
                .Select(d => (
                    Api: _apiOffsets[d.Api],
                    Assembly: _assemblyOffsets[d.Assembly],
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
            var assemblyPlatformSupport = _builder._assemblies
                .Select(a => (Assembly: a, Api: (IntermediaApi?)null, a.PlatformSupport));

            var apiPlatformSupport = _builder._assemblies
                .SelectMany(a => a.Declarations, (a, d) => (Assembly: a, Api: (IntermediaApi?)d.Key, d.Value.PlatformSupport));

            var entries = assemblyPlatformSupport
                .Concat(apiPlatformSupport)
                .Where(e => e.PlatformSupport is not null)
                .Select(e => (
                    Api: e.Api is null ? ApiOffset.Nil : _apiOffsets[e.Api],
                    Assembly: _assemblyOffsets[e.Assembly],
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
            var assemblyPreviewRequirements = _builder._assemblies
                .Select(a => (Assembly: a, Api: (IntermediaApi?)null, a.PreviewRequirement));

            var apiPreviewRequirements = _builder._assemblies
                .SelectMany(a => a.Declarations, (a, d) => (Assembly: a, Api: (IntermediaApi?)d.Key, d.Value.PreviewRequirement));

            var entries = assemblyPreviewRequirements
                .Concat(apiPreviewRequirements)
                .Where(e => e.PreviewRequirement is not null)
                .Select(e => (
                    Api: e.Api is null ? ApiOffset.Nil : _apiOffsets[e.Api],
                    Assembly: _assemblyOffsets[e.Assembly],
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
            var assemblyExperimental = _builder._assemblies
                .Select(a => (Assembly: a, Api: (IntermediaApi?)null, a.Experimental));

            var apiExperimental = _builder._assemblies
                .SelectMany(a => a.Declarations, (a, d) => (Assembly: a, Api: (IntermediaApi?)d.Key, d.Value.Experimental));

            var entries = assemblyExperimental
                .Concat(apiExperimental)
                .Where(e => e.Experimental is not null)
                .Select(e => (
                    Api: e.Api is null ? ApiOffset.Nil : _apiOffsets[e.Api],
                    Assembly: _assemblyOffsets[e.Assembly],
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

        private void WriteExtensionMethods()
        {
            var entries = _builder._apis
                .Where(a => a.Extensions is not null)
                .SelectMany(type => type.Extensions!, (type, extension) => (
                    ExtensionMethodGuid: extension.Fingerprint,
                    ExtendedType: _apiOffsets[type],
                    ExtensionMethod: _apiOffsets[extension.Method]))
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

        private class Memory
        {
            private readonly MemoryStream _data = new();
            private readonly BinaryWriter _writer;

            public Memory()
            {
                _writer = new BinaryWriter(_data, Encoding.UTF8, leaveOpen: true);
            }

            public void Seek(int offset)
            {
                _writer.Flush();
                _data.Position = offset;
            }

            public int GetLength()
            {
                _writer.Flush();
                return (int)_data.Length;
            }

            public int PeekInt32()
            {
                _writer.Flush();

                var location = _data.Position;
                var span = (Span<byte>) stackalloc byte[4];
                _data.ReadExactly(span);
                _data.Position = location;
                return BinaryPrimitives.ReadInt32LittleEndian(span);
            }

            public void CopyTo(Stream destination)
            {
                _writer.Flush();
                _data.Position = 0;
                _data.CopyTo(destination);
            }

            public void WriteStringOffset(StringOffset offset)
            {
                WriteInt32(offset.Value);
            }

            public void WriteBlobOffset(BlobOffset offset)
            {
                WriteInt32(offset.Value);
            }

            public void WriteFrameworkOffset(FrameworkOffset offset)
            {
                WriteInt32(offset.Value);
            }

            public void WritePackageOffset(PackageOffset offset)
            {
                WriteInt32(offset.Value);
            }

            public void WriteAssemblyOffset(AssemblyOffset offset)
            {
                WriteInt32(offset.Value);
            }

            public void WriteUsageSourceOffset(UsageSourceOffset offset)
            {
                WriteInt32(offset.Value);
            }

            public void WriteApiOffset(ApiOffset offset)
            {
                WriteInt32(offset.Value);
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

            private void WriteBytes(ReadOnlySpan<byte> bytes)
            {
                _writer.Write(bytes);
            }
        }

        private abstract class HeapOrTable
        {
            public Memory Memory { get; } = new();
        }

        private sealed class BlobHeap : HeapOrTable
        {
            private readonly List<BlobOffset> _assemblyPatchups = new();
            private readonly List<BlobOffset> _apiPatchups = new();
            private readonly List<(BlobOffset, IntermediateDeclaration)> _syntaxPatchups = new();
            private readonly Dictionary<Guid, BlobOffset> _storedPlatformSupport = new();

            private void WriteAssemblyPatchup(IntermediaAssembly assembly)
            {
                var offset = SeekEnd();
                _assemblyPatchups.Add(offset);
                Memory.WriteInt32(assembly.Index);
            }

            private void WriteApiPatchup(IntermediaApi api)
            {
                var offset = SeekEnd();
                _apiPatchups.Add(offset);
                Memory.WriteInt32(api.Index);
            }

            private void WriteSyntaxPatchup(IntermediateDeclaration declaration)
            {
                var offset = SeekEnd();
                _syntaxPatchups.Add((offset, declaration));
                Memory.WriteInt32(-1);
            }

            private BlobOffset SeekEnd()
            {
                var end = Memory.GetLength();
                Memory.Seek(end);
                return new BlobOffset(end);
            }

            public BlobOffset StoreAssemblies(IReadOnlyList<IntermediaAssembly> assemblies)
            {
                if (assemblies.Count == 0)
                    return BlobOffset.Nil;

                var result = SeekEnd();

                Memory.WriteInt32(assemblies.Count);
                foreach (var assembly in assemblies)
                    WriteAssemblyPatchup(assembly);

                return result;
            }

            public BlobOffset StoreAssemblies(IReadOnlyList<(IntermediateFramework, IntermediaAssembly)> assemblies,
                                              Dictionary<IntermediateFramework, FrameworkOffset> frameworkOffsets)
            {
                if (assemblies.Count == 0)
                    return BlobOffset.Nil;

                var result = SeekEnd();

                Memory.WriteInt32(assemblies.Count);
                foreach (var (framework, assembly) in assemblies)
                {
                    Memory.WriteFrameworkOffset(frameworkOffsets[framework]);
                    WriteAssemblyPatchup(assembly);
                }

                return result;
            }

            public BlobOffset StoreFrameworks(IReadOnlyList<IntermediateFramework> frameworks,
                                              Dictionary<IntermediateFramework, FrameworkOffset> frameworkOffsets)
            {
                if (frameworks.Count == 0)
                    return BlobOffset.Nil;

                var result = SeekEnd();

                Memory.WriteInt32(frameworks.Count);
                foreach (var framework in frameworks)
                    Memory.WriteFrameworkOffset(frameworkOffsets[framework]);

                return result;
            }

            public BlobOffset StorePackages(IReadOnlyList<(IntermediatePackage, IntermediateFramework)> packages,
                                            Dictionary<IntermediatePackage, PackageOffset> packageOffsets,
                                            Dictionary<IntermediateFramework, FrameworkOffset> frameworkOffsets)
            {
                if (packages.Count == 0)
                    return BlobOffset.Nil;

                var result = SeekEnd();

                Memory.WriteInt32(packages.Count);
                foreach (var (package, framework) in packages)
                {
                    Memory.WritePackageOffset(packageOffsets[package]);
                    Memory.WriteFrameworkOffset(frameworkOffsets[framework]);
                }

                return result;
            }

            public BlobOffset StoreApis(IReadOnlyList<IntermediaApi> apis)
            {
                if (apis.Count == 0)
                    return BlobOffset.Nil;

                var result = SeekEnd();

                Memory.WriteInt32(apis.Count);
                foreach (var api in apis)
                    WriteApiPatchup(api);

                return result;
            }

            public BlobOffset StoreDeclarations(IReadOnlyList<IntermediateDeclaration> declarations,
                                                Dictionary<IntermediaAssembly, AssemblyOffset> assemblyOffsets)
            {
                if (declarations.Count == 0)
                    return BlobOffset.Nil;

                var result = SeekEnd();

                Memory.WriteInt32(declarations.Count);
                foreach (var declaration in declarations)
                {
                    Memory.WriteAssemblyOffset(assemblyOffsets[declaration.Assembly]);
                    WriteSyntaxPatchup(declaration);
                }

                return result;
            }

            public BlobOffset StoreUsages(IReadOnlyList<(IntermediateUsageSource UsageSource, float Percentage)> usages,
                                          Dictionary<IntermediateUsageSource, UsageSourceOffset> usageSourceOffsets)
            {
                if (usages.Count == 0)
                    return BlobOffset.Nil;

                var result = SeekEnd();

                Memory.WriteInt32(usages.Count);
                foreach (var usage in usages)
                {
                    Memory.WriteUsageSourceOffset(usageSourceOffsets[usage.UsageSource]);
                    Memory.WriteSingle(usage.Percentage);
                }

                return result;
            }

            public BlobOffset StorePlatformSupport(IReadOnlyList<IntermediatePlatformSupport> platforms,
                                                   StringHeap stringHeap)
            {
                if (platforms.Count == 0)
                    return BlobOffset.Nil;

                var fingerprint = GetFingerprint(platforms);
                if (!_storedPlatformSupport.TryGetValue(fingerprint, out var result))
                {
                    result = SeekEnd();

                    Memory.WriteInt32(platforms.Count);
                    foreach (var platformSupport in platforms)
                    {
                        Memory.WriteStringOffset(stringHeap.Store(platformSupport.PlatformName));
                        Memory.WriteBool(platformSupport.IsSupported);
                    }
                    
                    _storedPlatformSupport.Add(fingerprint, result);
                }

                return result;

                static Guid GetFingerprint(IReadOnlyList<IntermediatePlatformSupport> platforms)
                {
                    using var stream = new MemoryStream();

                    using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                    {
                        foreach (var support in platforms.OrderBy(ps => ps.PlatformName, StringComparer.OrdinalIgnoreCase)
                                                         .ThenBy(ps => ps.IsSupported))
                        {
                            writer.Write(support.PlatformName.ToLowerInvariant());
                            writer.Write(support.IsSupported);
                        }
                    }

                    var bytes = MD5.HashData(stream);
                    return new Guid(bytes);
                }
            }
            
            public void PatchSyntaxes(CatalogBuilder builder, StringHeap stringHeap)
            {
                var syntaxOffsets = new Dictionary<string, BlobOffset>(StringComparer.Ordinal);

                foreach (var (patchOffset, declaration) in _syntaxPatchups)
                {
                    if (!syntaxOffsets.TryGetValue(declaration.Syntax, out var syntaxOffset))
                    {
                        syntaxOffset = StoreSyntax(declaration.Syntax);
                        syntaxOffsets.Add(declaration.Syntax, syntaxOffset);
                    }

                    Memory.Seek(patchOffset.Value);
                    Memory.WriteInt32(syntaxOffset.Value);
                }

                Memory.Seek(Memory.GetLength());

                BlobOffset StoreSyntax(string syntax)
                {
                    var markup = Markup.Parse(syntax);

                    var result = SeekEnd();
                    Memory.WriteInt32(markup.Parts.Length);

                    foreach (var part in markup.Parts)
                    {
                        var kind = (byte)part.Kind;
                        var text = stringHeap.Store(part.Text);

                        Memory.WriteByte(kind);
                        Memory.WriteStringOffset(text);

                        if (part.Kind == MarkupPartKind.Reference)
                        {
                            if (part.Reference is not null &&  builder._apiByFingerprint.TryGetValue(part.Reference.Value, out var api))
                                WriteApiPatchup(api);
                            else
                                Memory.WriteInt32(-1);
                        }
                    }

                    return result;
                }
            }

            public void PatchApiOffsets(IReadOnlyList<IntermediaApi> apis,
                                        Dictionary<IntermediaApi, ApiOffset> apiOffsets)
            {
                foreach (var patchOffset in _apiPatchups)
                {
                    Memory.Seek(patchOffset.Value);
                    var apiIndex = Memory.PeekInt32();
                    var api = apis[apiIndex];
                    var apiOffset = apiOffsets[api];
                    Memory.WriteApiOffset(apiOffset);
                }

                Memory.Seek(Memory.GetLength());
            }

            public void PatchAssemblyOffsets(IReadOnlyList<IntermediaAssembly> assemblies,
                                             Dictionary<IntermediaAssembly, AssemblyOffset> assemblyOffsets)
            {
                foreach (var patchOffset in _assemblyPatchups)
                {
                    Memory.Seek(patchOffset.Value);
                    var assemblyIndex = Memory.PeekInt32();
                    var assembly = assemblies[assemblyIndex];
                    var assemblyOffset = assemblyOffsets[assembly];
                    Memory.WriteAssemblyOffset(assemblyOffset);
                }

                Memory.Seek(Memory.GetLength());
            }
        }

        private sealed class StringHeap : HeapOrTable
        {
            private readonly Dictionary<string, StringOffset> _stringOffsets = new (StringComparer.Ordinal);

            public StringOffset Store(string text)
            {
                if (!_stringOffsets.TryGetValue(text, out var offset))
                {
                    offset = new StringOffset(Memory.GetLength());
                    Memory.WriteString(text);
                    _stringOffsets.Add(text, offset);
                }

                return offset;
            }
        }

        private sealed class PlatformTable : HeapOrTable
        {
            public void WriteRow(StringOffset name)
            {
                Memory.WriteStringOffset(name);
            }
        }

        private sealed class FrameworkTable : HeapOrTable
        {
            public FrameworkOffset WriteRow(StringOffset name,
                                            BlobOffset assemblies)
            {
                var offset = new FrameworkOffset(Memory.GetLength());

                Memory.WriteStringOffset(name);
                Memory.WriteBlobOffset(assemblies);

                return offset;
            }
        }

        private sealed class PackageTable : HeapOrTable
        {
            public PackageOffset WriteRow(StringOffset packageName,
                                          StringOffset packageVersion,
                                          BlobOffset assemblies)
            {
                var offset = new PackageOffset(Memory.GetLength());

                Memory.WriteStringOffset(packageName);
                Memory.WriteStringOffset(packageVersion);
                Memory.WriteBlobOffset(assemblies);

                return offset;
            }
        }

        private sealed class AssemblyTable : HeapOrTable
        {
            public AssemblyOffset WriteRow(Guid fingerprint,
                                           StringOffset name,
                                           StringOffset publicKeyToken,
                                           StringOffset version,
                                           BlobOffset rootApis,
                                           BlobOffset frameworks,
                                           BlobOffset packages)
            {
                var offset = new AssemblyOffset(Memory.GetLength());

                Memory.WriteGuid(fingerprint);
                Memory.WriteStringOffset(name);
                Memory.WriteStringOffset(publicKeyToken);
                Memory.WriteStringOffset(version);
                Memory.WriteBlobOffset(rootApis);
                Memory.WriteBlobOffset(frameworks);
                Memory.WriteBlobOffset(packages);

                return offset;
            }
        }

        private sealed class UsageSourceTable : HeapOrTable
        {
            public UsageSourceOffset WriteRow(StringOffset name,
                                              int dayNumber)
            {
                var offset = new UsageSourceOffset(Memory.GetLength());

                Memory.WriteStringOffset(name);
                Memory.WriteInt32(dayNumber);

                return offset;
            }
        }

        private sealed class ApiTable : HeapOrTable
        {
            public ApiOffset WriteRow(Guid fingerprint,
                                      byte kind,
                                      ApiOffset parent,
                                      StringOffset name,
                                      BlobOffset children,
                                      BlobOffset declarations,
                                      BlobOffset usages)
            {
                var offset = new ApiOffset(Memory.GetLength());

                Memory.WriteGuid(fingerprint);
                Memory.WriteByte(kind);
                Memory.WriteApiOffset(parent);
                Memory.WriteStringOffset(name);
                Memory.WriteBlobOffset(children);
                Memory.WriteBlobOffset(declarations);
                Memory.WriteBlobOffset(usages);

                return offset;
            }
        }

        private sealed class RootApiTable : HeapOrTable
        {
            public void WriteRow(ApiOffset api)
            {
                Memory.WriteApiOffset(api);
            }
        }

        private sealed class ObsoletionTable : HeapOrTable
        {
            public void WriteRow(ApiOffset api,
                                 AssemblyOffset assembly,
                                 StringOffset message,
                                 bool isError,
                                 StringOffset diagnosticId,
                                 StringOffset urlFormat)
            {
                Memory.WriteApiOffset(api);
                Memory.WriteAssemblyOffset(assembly);
                Memory.WriteStringOffset(message);
                Memory.WriteBool(isError);
                Memory.WriteStringOffset(diagnosticId);
                Memory.WriteStringOffset(urlFormat);
            }
        }

        private sealed class PlatformSupportTable : HeapOrTable
        {
            public void WriteRow(ApiOffset api,
                                 AssemblyOffset assembly,
                                 BlobOffset platforms)
            {
                Memory.WriteApiOffset(api);
                Memory.WriteAssemblyOffset(assembly);
                Memory.WriteBlobOffset(platforms);
            }
        }

        private sealed class PreviewRequirementTable : HeapOrTable
        {
            public void WriteRow(ApiOffset api,
                                 AssemblyOffset assembly,
                                 StringOffset message,
                                 StringOffset url)
            {
                Memory.WriteApiOffset(api);
                Memory.WriteAssemblyOffset(assembly);
                Memory.WriteStringOffset(message);
                Memory.WriteStringOffset(url);
            }
        }

        private sealed class ExperimentalTable : HeapOrTable
        {
            public void WriteRow(ApiOffset api,
                                 AssemblyOffset assembly,
                                 StringOffset diagnosticId,
                                 StringOffset urlFormat)
            {
                Memory.WriteApiOffset(api);
                Memory.WriteAssemblyOffset(assembly);
                Memory.WriteStringOffset(diagnosticId);
                Memory.WriteStringOffset(urlFormat);
            }
        }

        private sealed class ExtensionMethodTable : HeapOrTable
        {
            public void WriteRow(Guid extensionMethodGuid,
                                 ApiOffset extendedType,
                                 ApiOffset extensionMethod)
            {
                Memory.WriteGuid(extensionMethodGuid);
                Memory.WriteApiOffset(extendedType);
                Memory.WriteApiOffset(extensionMethod);
            }
        }
    }
}