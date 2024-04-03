#nullable enable

using System.Collections.Frozen;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using NuGet.Frameworks;
using Terrajobst.ApiCatalog;

namespace ApisOfDotNet.Shared;

public partial class ApiOutline
{
    [Parameter]
    public required IApiOutlineOwner Owner { get; set; }

    [Parameter]
    public required ApiOutlineData Data { get; set; } 
}

public interface IApiOutlineOwner
{
    string Link(ApiModel api);
    string Link(ExtensionMethodModel api);
    bool IsIncluded(ApiModel api);
    bool IsSupported(ApiModel api);
}

public abstract class ApiFilter
{
    public static ApiFilter Everything { get; } = new EverythingApiFilter();

    public static ApiFilter ForAssembly(AssemblyModel assembly)
    {
        return new AssemblyApiFilter(assembly);
    }

    public static ApiFilter ForFramework(FrameworkModel framework)
    {
        return new FrameworkApiFilter(framework);
    }

    public static ApiFilter ForPackage(PackageModel package)
    {
        return new PackageApiFilter(package);
    }

    public abstract bool IsIncluded(ApiModel api);

    public bool IsIncluded(ExtensionMethodModel api)
    {
        return IsIncluded(api.ExtensionMethod);
    }

    private sealed class EverythingApiFilter : ApiFilter
    {
        public override bool IsIncluded(ApiModel api)
        {
            return true;
        }
    }

    private sealed class AssemblyApiFilter : ApiFilter
    {
        private readonly AssemblyModel _assembly;

        public AssemblyApiFilter(AssemblyModel assembly)
        {
            _assembly = assembly;
        }

        public override bool IsIncluded(ApiModel api)
        {
            return api.Declarations.Any(d => d.Assembly == _assembly);
        }
    }

    private sealed class FrameworkApiFilter : ApiFilter
    {
        private readonly FrozenSet<int> _assemblies;

        public FrameworkApiFilter(FrameworkModel framework)
        {
            _assemblies = framework.Assemblies.Select(a => a.Id).ToFrozenSet();
        }

        public override bool IsIncluded(ApiModel api)
        {
            return api.Declarations.Any(d => _assemblies.Contains(d.Assembly.Id));
        }
    }

    private sealed class PackageApiFilter : ApiFilter
    {
        private readonly FrozenSet<int> _assemblies;

        public PackageApiFilter(PackageModel package)
        {
            _assemblies = package.Assemblies.Select(a => a.Assembly.Id).ToFrozenSet();
        }

        public override bool IsIncluded(ApiModel api)
        {
            return api.Declarations.Any(d => _assemblies.Contains(d.Assembly.Id));
        }
    }
}

public abstract class ApiOutlineData
{
    public static ApiOutlineRoot CreateRoot(IEnumerable<ApiModel> roots)
    {
        return new ApiOutlineRoot(roots.ToImmutableArray());
    }

    public static ApiOutlineData CreateRoot(FrameworkModel framework)
    {
        var roots = framework.Assemblies.SelectMany(a => a.RootApis).ToHashSet().Order();
        return CreateRoot(roots);
    }

    public static ApiOutlineData CreateRoot(PackageModel package)
    {
        var roots = package.Assemblies.SelectMany(a => a.Assembly.RootApis).ToHashSet().Order();
        return CreateRoot(roots);
    }

    public static ApiOutlineNode? CreateNode(ApiCatalogModel catalog, string text)
    {
        return Guid.TryParse(text, out var guid) ? CreateNode(catalog, guid) : null;
    }

    public static ApiOutlineNode? CreateNode(ApiCatalogModel catalog, Guid guid)
    {
        ApiModel api;
        ExtensionMethodModel? extensionMethod;

        try
        {
            api = catalog.GetApiByGuid(guid);
            extensionMethod = null;
        }
        catch (KeyNotFoundException)
        {
            try
            {
                extensionMethod = catalog.GetExtensionMethodByGuid(guid);
                api = extensionMethod.Value.ExtensionMethod;
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        ApiModel parent;
        
        if (extensionMethod is not null)
        {
            parent = extensionMethod.Value.ExtendedType;
        }
        else if (api.Kind.IsMember() && api.Parent is not null)
        {
            parent = api.Parent.Value;
        }
        else
        {
            parent = api;
        }

        var breadcrumbs = extensionMethod is not null
            ? extensionMethod.Value.ExtendedType.AncestorsAndSelf().Reverse().Append(extensionMethod.Value.ExtensionMethod).ToImmutableArray()
            : api.AncestorsAndSelf().Reverse().ToImmutableArray();

        return new ApiOutlineNode(breadcrumbs, parent, api, extensionMethod);
    }
}

public sealed class ApiOutlineRoot : ApiOutlineData
{
    public ApiOutlineRoot(ImmutableArray<ApiModel> entries)
    {
        Entries = entries;
    }

    public ImmutableArray<ApiModel> Entries { get; }
}

public sealed class ApiOutlineNode : ApiOutlineData
{
    public ApiOutlineNode(ImmutableArray<ApiModel> breadcrumbs, ApiModel parent, ApiModel api, ExtensionMethodModel? extensionMethod)
    {
        Breadcrumbs = breadcrumbs;
        Parent = parent;
        Api = api;
        ExtensionMethod = extensionMethod;
    }

    public ImmutableArray<ApiModel> Breadcrumbs { get; }

    public ApiModel Parent { get; }

    public ApiModel Api { get; }

    public ExtensionMethodModel? ExtensionMethod { get; }
}