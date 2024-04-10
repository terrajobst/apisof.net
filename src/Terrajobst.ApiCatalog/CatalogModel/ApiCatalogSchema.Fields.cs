namespace Terrajobst.ApiCatalog;

internal static partial class ApiCatalogSchema
{
    // Note: The order of these fields is important due to initialization dependencies.
    //       The correct order is primitives, structures, and then tables.

    // Definitions of array primitives

    public static readonly Field<FrameworkModel> FrameworkElement = new FrameworkField();

    public static readonly Field<AssemblyModel> AssemblyElement = new AssemblyField();

    public static readonly Field<ApiModel> ApiElement = new ApiField();

    // Definitions of array structures

    public static readonly ApiDeclarationLayout ApiDeclarationStructure = new(new LayoutBuilder(ApiCatalogHeapOrTable.BlobHeap));

    public static readonly ApiUsageLayout ApiUsageStructure = new(new LayoutBuilder(ApiCatalogHeapOrTable.BlobHeap));

    public static readonly PackageAssemblyTupleLayout PackageAssemblyTuple = new(new LayoutBuilder(ApiCatalogHeapOrTable.BlobHeap));

    public static readonly AssemblyPackageTupleLayout AssemblyPackageTuple = new(new LayoutBuilder(ApiCatalogHeapOrTable.BlobHeap));

    public static readonly PlatformIsSupportedTupleLayout PlatformIsSupportedTuple = new(new LayoutBuilder(ApiCatalogHeapOrTable.BlobHeap));

    // Definition of Tables

    public static readonly PlatformRowLayout PlatformRow = new(new LayoutBuilder(ApiCatalogHeapOrTable.PlatformTable));
    public static readonly FrameworkRowLayout FrameworkRow = new(new LayoutBuilder(ApiCatalogHeapOrTable.FrameworkTable));
    public static readonly PackageRowLayout PackageRow = new(new LayoutBuilder(ApiCatalogHeapOrTable.PackageTable));
    public static readonly AssemblyRowLayout AssemblyRow = new(new LayoutBuilder(ApiCatalogHeapOrTable.AssemblyTable));
    public static readonly UsageSourceRowLayout UsageSourceRow = new(new LayoutBuilder(ApiCatalogHeapOrTable.UsageSourceTable));
    public static readonly ApiRowLayout ApiRow = new(new LayoutBuilder(ApiCatalogHeapOrTable.ApiTable));
    public static readonly RootApiRowLayout RootApiRow = new(new LayoutBuilder(ApiCatalogHeapOrTable.RootApiTable));
    public static readonly ExtensionMethodRowLayout ExtensionMethodRow = new(new LayoutBuilder(ApiCatalogHeapOrTable.ExtensionMethodTable));
    public static readonly ObsoletionRowLayout ObsoletionRow = new(new LayoutBuilder(ApiCatalogHeapOrTable.ObsoletionTable));
    public static readonly PlatformSupportRowLayout PlatformSupportRow = new(new LayoutBuilder(ApiCatalogHeapOrTable.PlatformSupportTable));
    public static readonly PreviewRequirementRowLayout PreviewRequirementRow = new(new LayoutBuilder(ApiCatalogHeapOrTable.PreviewRequirementTable));
    public static readonly ExperimentalRowLayout ExperimentalRow = new(new LayoutBuilder(ApiCatalogHeapOrTable.ExperimentalTable));
}