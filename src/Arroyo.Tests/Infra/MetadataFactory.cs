using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp;

using System.Diagnostics;
using System.Text;
using Basic.Reference.Assemblies;

namespace Arroyo.Tests.Infra;

internal static class MetadataFactory
{
    public static MetadataAssembly CreateAssembly(string source)
    {
        return CreateCompilation(source).ToMetadataAssembly();
    }

    public static MetadataModule CreateModule(string source)
    {
        var compilation = CreateCompilation(source);
        compilation = compilation.WithOptions(compilation.Options.WithOutputKind(OutputKind.NetModule));
        
        return compilation.ToMetadataModule();
    }

    public static MetadataFile ToMetadataFile(this CSharpCompilation compilation)
    {
        // Note: Don't dispose the memory stream here; it's owned by the MetadataFile. 
        var memoryStream = new MemoryStream();
        var emitResults = compilation.Emit(memoryStream);

        if (!emitResults.Success)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Errors in compilation:");
            sb.AppendLine();

            foreach (var d in emitResults.Diagnostics)
                sb.AppendLine(d.ToString());

            throw new Exception(sb.ToString());
        }

        memoryStream.Position = 0;

        var result = MetadataFile.Open(memoryStream);
        Debug.Assert(result is not null);

        return result;
    }

    public static MetadataAssembly ToMetadataAssembly(this CSharpCompilation compilation)
    {
        return (MetadataAssembly)compilation.ToMetadataFile();
    }

    public static MetadataModule ToMetadataModule(this CSharpCompilation compilation)
    {
        return (MetadataModule)compilation.ToMetadataFile();
    }

    public static IAssemblySymbol CreateRoslynAssembly(string source)
    {
        var compilation = CreateCompilation(source);
        return compilation.Assembly;
    }

    public static IAssembly CreateCciAssembly(string source)
    {
        var compilation = CreateCompilation(source);

        using var memoryStream = new MemoryStream();
        var emitResults = compilation.Emit(memoryStream);

        if (!emitResults.Success)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Errors in compilation:");
            sb.AppendLine();

            foreach (var d in emitResults.Diagnostics)
                sb.AppendLine(d.ToString());

            throw new Exception(sb.ToString());
        }

        memoryStream.Position = 0;

        var host = new Microsoft.Cci.Extensions.HostEnvironment();
        return host.LoadAssemblyFrom("dummy", memoryStream);
    }

    public static CSharpCompilation CreateCompilation(string source, IEnumerable<CSharpCompilation>? dependencies = null)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var syntaxTrees = new[] { syntaxTree };
        var references = new List<MetadataReference> {
            Net60.References.SystemRuntime,
            Net60.References.SystemRuntimeInteropServices,
            Net60.References.SystemCollections
        };

        if (dependencies is not null)
        {
            foreach (var dependency in dependencies)
                references.Add(dependency.ToMetadataReference());
        }

        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);
        var compilation = CSharpCompilation.Create("dummy", syntaxTrees, references, options);
        return compilation;
    }
}