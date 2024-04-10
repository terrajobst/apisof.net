namespace Terrajobst.ApiCatalog;

internal enum ApiCatalogHeapOrTable
{
    StringHeap,
    BlobHeap,
    PlatformTable,
    FrameworkTable,
    PackageTable,
    AssemblyTable,
    UsageSourceTable,
    ApiTable,
    RootApiTable,
    ExtensionMethodTable,
    ObsoletionTable,
    PlatformSupportTable,
    PreviewRequirementTable,
    ExperimentalTable
}