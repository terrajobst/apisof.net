
using System.Collections.Immutable;

namespace Terrajobst.ApiCatalog;

partial class Area
{
    public static ImmutableArray<Area> All { get; } =
    [
        // Foundational

        new Area("System")
        {
            Include =
            [
                AreaMatcher.DefinedInPlatformNeutralFramework(),
                AreaMatcher.DefinedInFrameworkFamily(".NETFramework"),
                AreaMatcher.DefinedInPackage("System.*"),
                AreaMatcher.DefinedInPackage("Microsoft.Bcl.*"),
            ]
        },
        new Area("Extensions")
        {
            Include =
            [
                AreaMatcher.DefinedInPackage("Microsoft.Extensions.*"),
                AreaMatcher.DefinedInAssembly("Microsoft.Extensions.Localization")
            ]
        },
        new Area("Diagnostics")
        {
            Include =
            [
                AreaMatcher.DefinedInPackage("Microsoft.Diagnostics.*")
            ]
        },
        new Area("CoreWCF")
        {
            Include =
            [
                AreaMatcher.DefinedInPackage("CoreWCF.*"),
            ]
        },
        new Area("Newtonsoft.Json")
        {
            Include =
            [
                AreaMatcher.DefinedInPackage("Newtonsoft.Json.*"),
            ]
        },

        // Build & Compilation

        new Area("MSBuild")
        {
            Include =
            [
                AreaMatcher.DefinedInPackage("Microsoft.Build.*"),
            ]
        },
        new Area("C# and VB")
        {
            Include =
            [
                AreaMatcher.DefinedInPackage("Microsoft.CodeAnalysis.*"),
                AreaMatcher.DefinedInPackage("Microsoft.CodeDom.*"),
                AreaMatcher.DefinedInPackage("Microsoft.CSharp.*"),
                AreaMatcher.DefinedInAssembly("Microsoft.CSharp.*"),
                AreaMatcher.DefinedInAssembly("Microsoft.VisualBasic.*"),
                AreaMatcher.DefinedInAssembly("System.CodeDom.*"),
            ]
        },
        new Area("F#")
        {
            Include =
            [
                AreaMatcher.DefinedInPackage("FSharp.*"),
            ]
        },

        // Web & Cloud

        new Area("Azure")
        {
            Include =
            [
                AreaMatcher.DefinedInPackage("Azure.*"),
                AreaMatcher.DefinedInPackage("Microsoft.Azure.*"),
                AreaMatcher.DefinedInPackage("Microsoft.WindowsAzure.*"),
            ]
        },
        new Area("ASP.NET")
        {
            Include =
            [
                AreaMatcher.DefinedInPackage("Microsoft.AspNet.*"),
                AreaMatcher.DefinedInPackage("Microsoft.Web.*"),
                AreaMatcher.DefinedInPackage("Microsoft.Net.Http.Server"),
            ]
        },
        new Area("ASP.NET Core")
        {
            Include =
            [
                AreaMatcher.DefinedInPackage("Microsoft.AspNetCore.*"),
                AreaMatcher.DefinedInPackage("Microsoft.JSInterop.WebAssembly.*"),
            ]
        },
        new Area("Aspire")
        {
            Include =
            [
                AreaMatcher.DefinedInPackage("Aspire.*"),
            ]
        },
        new Area("YARP")
        {
            Include =
            [
                AreaMatcher.DefinedInPackage("Microsoft.ReverseProxy.*"),
            ]
        },

        // Databases

        new Area("EF")
        {
            Include =
            [
                AreaMatcher.DefinedInPackage("EntityFramework.*"),
            ]
        },
        new Area("EF Core")
        {
            Include =
            [
                AreaMatcher.DefinedInPackage("Microsoft.EntityFrameworkCore.*"),
            ]
        },
        new Area("Database")
        {
            Include =
            [
                AreaMatcher.DefinedInPackage("Microsoft.Data.*"),
                AreaMatcher.DefinedInPackage("System.Data.*"),
                AreaMatcher.DefinedInPackage("System.Spatial.*"),
            ]
        },

        // Desktop & Mobile

        new Area("Android")
        {
            Include =
            [
                AreaMatcher.DefinedInPlatformSpecificFramework("android"),
            ]
        },
        new Area("iOS")
        {
            Include =
            [
                AreaMatcher.DefinedInPlatformSpecificFramework("ios"),
            ]
        },
        new Area("MacOS")
        {
            Include =
            [
                AreaMatcher.DefinedInPlatformSpecificFramework("macos"),
            ]
        },
        new Area("MacCatalyst")
        {
            Include =
            [
                AreaMatcher.DefinedInPlatformSpecificFramework("maccatalyst"),
            ]
        },
        new Area("tvOS")
        {
            Include =
            [
                AreaMatcher.DefinedInPlatformSpecificFramework("tvos"),
            ]
        },
        new Area("Windows")
        {
            Include =
            [
                AreaMatcher.DefinedInPlatformSpecificFramework("windows"),
                AreaMatcher.DefinedInPackage("Microsoft.WindowsAppSDK.*"),
            ]
        },
        new Area("MAUI")
        {
            Include =
            [
                AreaMatcher.DefinedInPackage("Microsoft.MAUI.*"),
            ]
        },

        // IOT

        new Area("IOT")
        {
            Include =
            [
                AreaMatcher.DefinedInPackage("Iot.*"),
            ]
        },

        // Machine Learning

        new Area("ML.NET")
        {
            Include =
            [
                AreaMatcher.DefinedInPackage("Microsoft.ML.*"),
            ]
        },
        new Area("Spark")
        {
            Include =
            [
                AreaMatcher.DefinedInPackage("Microsoft.Spark.*"),
            ]
        },
    ];
}
