using System.Collections.Immutable;
using System.Reflection;

namespace Arroyo;

public sealed class MetadataAssemblyIdentity
{
    private readonly string _name;
    private readonly string _culture;
    private readonly AssemblyFlags _flags;
    private readonly AssemblyHashAlgorithm _hashAlgorithm;
    private readonly ImmutableArray<byte> _publicKey;
    private readonly Version _version;

    private ImmutableArray<byte> _publicKeyToken;

    internal MetadataAssemblyIdentity(string name,
                                      string culture,
                                      AssemblyFlags flags,
                                      AssemblyHashAlgorithm hashAlgorithm,
                                      ImmutableArray<byte> publicKeyOrToken,
                                      bool hasPublicKey,
                                      Version version)
    {
        _name = name;
        _culture = culture;
        _flags = flags;
        _hashAlgorithm = hashAlgorithm;
        _version = version;

        if (hasPublicKey)
        {
            _publicKey = publicKeyOrToken;
            _publicKeyToken = default;
        }
        else
        {
            _publicKey = ImmutableArray<byte>.Empty;
            _publicKeyToken = publicKeyOrToken;
        }
    }

    private MetadataAssemblyIdentity(AssemblyName assemblyName)
    {
        _name = assemblyName.Name ?? string.Empty;
        _culture = assemblyName.CultureName ?? string.Empty;
        _flags = (AssemblyFlags)assemblyName.Flags;
        _hashAlgorithm = (AssemblyHashAlgorithm)assemblyName.HashAlgorithm;
        var publicKey = assemblyName.GetPublicKey();
        _publicKey = publicKey?.ToImmutableArray() ?? ImmutableArray<byte>.Empty;
        var publicKeyToken = assemblyName.GetPublicKeyToken();
        _publicKeyToken = publicKeyToken?.ToImmutableArray() ?? ImmutableArray<byte>.Empty;
        _version = assemblyName.Version ?? new Version();
    }

    internal static MetadataAssemblyIdentity Parse(string qualifiedName)
    {
        try
        {
            var assemblyName = new AssemblyName(qualifiedName);
            return new MetadataAssemblyIdentity(assemblyName);
        }
        catch (FileLoadException)
        {
            throw new FormatException($"The name '{qualifiedName}' isn't a valid assembly name.");
        }
    }

    public string Name
    {
        get { return _name; }
    }

    public string Culture
    {
        get { return _culture; }
    }

    public AssemblyFlags Flags
    {
        get { return _flags; }
    }

    public AssemblyHashAlgorithm HashAlgorithm
    {
        get { return _hashAlgorithm; }
    }

    public bool HasPublicKey
    {
        get { return _flags.IsFlagSet(AssemblyFlags.PublicKey); }
    }

    public bool IsRetargetable
    {
        get { return _flags.IsFlagSet(AssemblyFlags.Retargetable); }
    }

    public bool DisableJitCompileOptimizer
    {
        get { return _flags.IsFlagSet(AssemblyFlags.DisableJitCompileOptimizer); }
    }

    public bool EnableJitCompileTracking
    {
        get { return _flags.IsFlagSet(AssemblyFlags.EnableJitCompileTracking); }
    }

    public AssemblyContentType ContentType
    {
        get { return _flags.GetContentType(); }
    }

    public ImmutableArray<byte> PublicKey
    {
        get { return _publicKey; }
    }

    public ImmutableArray<byte> PublicKeyToken
    {
        get
        {
            if (_publicKeyToken.IsDefault)
            {
                var publicKeyToken = ComputePublicKeyToken();
                ImmutableInterlocked.InterlockedInitialize(ref _publicKeyToken, publicKeyToken);
            }

            return _publicKeyToken;
        }
    }

    public Version Version
    {
        get { return _version; }
    }

    private ImmutableArray<byte> ComputePublicKeyToken()
    {
        return PublicKey.ToPublicKeyToken(HashAlgorithm);
    }

    public override string ToString()
    {
        return $"{Name}, Version={Version}, PublicKeyToken={PublicKeyToken.ToHexString()}, Culture={Culture}";
    }
}