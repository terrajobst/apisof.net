namespace Terrajobst.ApiCatalog;

public abstract class AreaMatcher
{
    private AreaMatcher()
    {
    }

    public abstract bool Matches(ApiModel api);

    private static bool ExpressionMatches(string expression, string name)
    {
        var isWildcard = expression.EndsWith(".*", StringComparison.Ordinal);

        if (!isWildcard)
            return string.Equals(name, expression, StringComparison.OrdinalIgnoreCase);

        return name.AsSpan().Equals(expression.AsSpan(0, expression.Length - 2), StringComparison.OrdinalIgnoreCase) ||
               name.AsSpan().StartsWith(expression.AsSpan(0, expression.Length - 1), StringComparison.OrdinalIgnoreCase);
    }

    public static AreaMatcher DefinedInPlatformNeutralFramework()
    {
        return DefinedInPlatformNeutralFrameworkMatcher.Instance;
    }

    public static AreaMatcher DefinedInPlatformSpecificFramework(string platform)
    {
        ThrowIfNullOrEmpty(platform);

        return new DefinedInPlatformSpecificFrameworkMatcher(platform);
    }

    public static AreaMatcher DefinedInFrameworkFamily(string identifier)
    {
        ThrowIfNullOrEmpty(identifier);

        return new DefinedInFrameworkFamilyMatcher(identifier);
    }

    public static AreaMatcher DefinedInPackage(string nameExpression)
    {
        ThrowIfNullOrEmpty(nameExpression);

        return new DefinedInPackageMatcher(nameExpression);
    }

    public static AreaMatcher DefinedInAssembly(string nameExpression)
    {
        ThrowIfNullOrEmpty(nameExpression);

        return new DefinedInAssemblyMatcher(nameExpression);
    }

    private sealed class DefinedInPlatformNeutralFrameworkMatcher : AreaMatcher
    {
        public static DefinedInPlatformNeutralFrameworkMatcher Instance { get; } = new();

        private DefinedInPlatformNeutralFrameworkMatcher()
        {
        }

        public override bool Matches(ApiModel api)
        {
            foreach (var d in api.Declarations)
            {
                foreach (var f in d.Assembly.Frameworks)
                {
                    // Ignore .NET Core 2.x because the archived frameworks contains a lot of 3rd party code
                    if (f.NuGetFramework.Version.Major == 2 && string.Equals(f.NuGetFramework.Framework, ".NETCoreApp"))
                        continue;

                    // Let's ignore older platform
                    if (!f.NuGetFramework.IsRelevantForCatalog())
                        continue;

                    if (f.NuGetFramework.Version.Major >= 5 && f.NuGetFramework.IsPlatformNeutral())
                        return true;
                }
            }

            return false;
        }
    }

    private sealed class DefinedInPlatformSpecificFrameworkMatcher : AreaMatcher
    {
        private readonly string _identifier;

        public DefinedInPlatformSpecificFrameworkMatcher(string platform)
        {
            _identifier = platform;
        }

        public override bool Matches(ApiModel api)
        {
            foreach (var d in api.Declarations)
            {
                foreach (var f in d.Assembly.Frameworks)
                {
                    if (string.Equals(f.NuGetFramework.Platform, _identifier))
                        return true;
                }
            }

            return false;
        }
    }

    private sealed class DefinedInFrameworkFamilyMatcher : AreaMatcher
    {
        private readonly string _identifier;

        public DefinedInFrameworkFamilyMatcher(string identifier)
        {
            _identifier = identifier;
        }

        public override bool Matches(ApiModel api)
        {
            foreach (var d in api.Declarations)
            {
                foreach (var f in d.Assembly.Frameworks)
                {
                    if (string.Equals(f.NuGetFramework.Framework, _identifier))
                        return true;
                }
            }

            return false;
        }
    }

    private sealed class DefinedInPackageMatcher : AreaMatcher
    {
        private readonly string _nameExpression;

        public DefinedInPackageMatcher(string nameExpression)
        {
            _nameExpression = nameExpression;
        }

        public override bool Matches(ApiModel api)
        {
            foreach (var d in api.Declarations)
            {
                foreach (var (p, _) in d.Assembly.Packages)
                {
                    if (ExpressionMatches(_nameExpression, p.Name))
                        return true;
                }
            }

            return false;
        }
    }

    private sealed class DefinedInAssemblyMatcher : AreaMatcher
    {
        private readonly string _nameExpression;

        public DefinedInAssemblyMatcher(string nameExpression)
        {
            _nameExpression = nameExpression;
        }

        public override bool Matches(ApiModel api)
        {
            foreach (var d in api.Declarations)
            {
                if (ExpressionMatches(_nameExpression, d.Assembly.Name))
                    return true;
            }

            return false;
        }
    }
}
