namespace Terrajobst.ApiCatalog.Features;

public sealed class FeatureUsageSource
{
    public FeatureUsageSource(string name, DateOnly date)
    {
        ThrowIfNullOrEmpty(name);

        Name = name;
        Date = date;
    }

    public string Name { get; }

    public DateOnly Date { get; }

    public int ReferenceUnitCount
    {
        get
        {
            return Name switch {
                "NetFx Compat Lab" => 403,
                "nuget.org" => 682123,
                "Upgrade Planner" => 25992
            };
        }
    }

    public IReadOnlyList<(int Version, int ReferenceUnitCount)> VersionDistribution
    {
        get
        {
            return Name switch {
                "NetFx Compat Lab" => DistributionNetFxCompatLab,
                "nuget.org" => DistributionNuGet,
                "Upgrade Planner" => DistributionUpgradePlanner
            };
        }
    }

    public int SupportedCollectionVersion
    {
        get
        {
            return Name switch {
                "NetFx Compat Lab" => 0,
                "nuget.org" => 2,
                "Upgrade Planner" => 0
            };
        }
    }
    
    private static (int, int)[] DistributionNetFxCompatLab = [(0, 403)];

    private static (int, int)[] DistributionUpgradePlanner = [(0, 25992)];

    private static (int, int)[] DistributionNuGet = [(0, 311287), (2, 370569)];
}