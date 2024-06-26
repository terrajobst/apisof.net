﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Terrajobst.ApiCatalog.Tests;

internal sealed class FluentCatalogBuilder
{
    private readonly InMemoryStore _indexStore = new();

    public FluentCatalogBuilder AddFramework(string name, Action<FrameworkBuilder> action)
    {
        var builder = new FrameworkBuilder(name);
        action(builder);
        var entry = builder.Build();
        _indexStore.Store(entry);
        return this;
    }

    public FluentCatalogBuilder AddPackage(string id, string version, Action<PackageBuilder> action)
    {
        var builder = new PackageBuilder(id, version);
        action(builder);
        var entry = builder.Build();
        _indexStore.Store(entry);
        return this;
    }

    public async Task<ApiCatalogModel> BuildAsync()
    {
        var builder = new CatalogBuilder();
        builder.Index(_indexStore);
        return await builder.BuildAsync();
    }

    public sealed class FrameworkBuilder
    {
        private readonly string _frameworkName;
        private readonly List<FrameworkAssemblyEntry> _assemblies = new();

        public FrameworkBuilder(string frameworkName)
        {
            _frameworkName = frameworkName;
        }

        public FrameworkBuilder AddAssembly(string name, string source)
        {
            var referencePaths = new[] {
                typeof(object).Assembly.Location,
            };

            var references = referencePaths.Select(p => MetadataReference.CreateFromFile(p)).ToArray();

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: true,
                optimizationLevel: OptimizationLevel.Release
            );

            var compilation = CSharpCompilation.Create(name,
                new[] { CSharpSyntaxTree.ParseText(source) },
                references,
                options
            );

            using var peStream = new MemoryStream();

            var result = compilation.Emit(peStream);
            if (!result.Success)
            {
                var diagnostics = string.Join(Environment.NewLine, result.Diagnostics);
                var message = $"Compilation has errors{Environment.NewLine}{diagnostics}";
                throw new Exception(message);
            }

            peStream.Position = 0;

            var reference = MetadataReference.CreateFromStream(peStream, filePath: $"{name}.dll");
            var context = MetadataContext.Create(new[] { reference }, references);
            var entry = AssemblyEntry.Create(context.Assemblies.Single());
            var frameworkAssembly = new FrameworkAssemblyEntry(null, Array.Empty<string>(), entry);
            _assemblies.Add(frameworkAssembly);

            return this;
        }

        public FrameworkEntry Build()
        {
            var assemblies = _assemblies.ToArray();
            return FrameworkEntry.Create(_frameworkName, assemblies);
        }
    }

    public sealed class PackageBuilder
    {
        private readonly string _id;
        private readonly string _version;
        private readonly List<PackageAssemblyEntry> _assemblies = new();

        public PackageBuilder(string id, string version)
        {
            _id = id;
            _version = version;
        }

        public PackageBuilder AddFramework(string name, Action<FrameworkBuilder> action)
        {
            var builder = new FrameworkBuilder(name);
            action(builder);
            var frameworkEntry = builder.Build();

            foreach (var assembly in frameworkEntry.Assemblies)
                _assemblies.Add(new PackageAssemblyEntry(frameworkEntry.FrameworkName, assembly.Assembly));
            return this;
        }

        public PackageEntry Build()
        {
            var frameworks = _assemblies.ToArray();
            return PackageEntry.Create(_id, _version, frameworks);
        }
    }
}