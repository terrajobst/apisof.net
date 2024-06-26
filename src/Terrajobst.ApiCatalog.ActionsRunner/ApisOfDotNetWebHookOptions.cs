using System.ComponentModel.DataAnnotations;

namespace Terrajobst.ApiCatalog.ActionsRunner;

public sealed class ApisOfDotNetWebHookOptions
{
    [Required]
    public required string ApisOfDotNetWebHookSecret { get; init; }
}