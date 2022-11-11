using System.Collections.Immutable;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Arroyo;

internal static class MetadataExtensions
{
    public static readonly MetadataType UninitializedType = MetadataUninitializedType.Instance;
    public static readonly object UninitializedValue = new();
    public const MetadataAccessibility UninitializedAccessibility = (MetadataAccessibility)(-1);
    public const MethodKind UninitializedMethodKind = (MethodKind)(-1);
    public const TypeKind UninitializedTypeKind = (TypeKind)(-1);

    public static MetadataAccessibility GetAccessibility(this TypeAttributes attributes)
    {
        var accessibility = attributes & TypeAttributes.VisibilityMask;

        switch (accessibility)
        {
            case TypeAttributes.NotPublic:
                return MetadataAccessibility.Assembly;
            case TypeAttributes.Public:
                return MetadataAccessibility.Public;
            case TypeAttributes.NestedPublic:
                return MetadataAccessibility.Public;
            case TypeAttributes.NestedPrivate:
                return MetadataAccessibility.Private;
            case TypeAttributes.NestedFamily:
                return MetadataAccessibility.Family;
            case TypeAttributes.NestedAssembly:
                return MetadataAccessibility.Assembly;
            case TypeAttributes.NestedFamANDAssem:
                return MetadataAccessibility.FamilyAndAssembly;
            case TypeAttributes.NestedFamORAssem:
                return MetadataAccessibility.FamilyOrAssembly;
            default:
                throw new ArgumentOutOfRangeException("attributes");
        }
    }

    public static bool IsFlagSet(this TypeAttributes attributes, TypeAttributes flag)
    {
        return (attributes & flag) == flag;
    }

    public static MetadataAccessibility GetAccessibility(this FieldAttributes attributes)
    {
        var accessibility = attributes & FieldAttributes.FieldAccessMask;

        switch (accessibility)
        {
            case FieldAttributes.Private:
                return MetadataAccessibility.Private;
            case FieldAttributes.FamANDAssem:
                return MetadataAccessibility.FamilyAndAssembly;
            case FieldAttributes.Assembly:
                return MetadataAccessibility.Assembly;
            case FieldAttributes.Family:
                return MetadataAccessibility.Family;
            case FieldAttributes.FamORAssem:
                return MetadataAccessibility.FamilyOrAssembly;
            case FieldAttributes.Public:
                return MetadataAccessibility.Public;
            default:
                throw new ArgumentOutOfRangeException("attributes");
        }
    }

    public static bool IsFlagSet(this FieldAttributes attributes, FieldAttributes flag)
    {
        return (attributes & flag) == flag;
    }

    public static MetadataAccessibility GetAccessibility(this MethodAttributes attributes)
    {
        var accessibility = attributes & MethodAttributes.MemberAccessMask;

        switch (accessibility)
        {
            case MethodAttributes.Private:
                return MetadataAccessibility.Private;
            case MethodAttributes.FamANDAssem:
                return MetadataAccessibility.FamilyAndAssembly;
            case MethodAttributes.Assembly:
                return MetadataAccessibility.Assembly;
            case MethodAttributes.Family:
                return MetadataAccessibility.Family;
            case MethodAttributes.FamORAssem:
                return MetadataAccessibility.FamilyOrAssembly;
            case MethodAttributes.Public:
                return MetadataAccessibility.Public;
            default:
                throw new ArgumentOutOfRangeException("attributes");
        }
    }

    public static bool IsFlagSet(this MethodAttributes attributes, MethodAttributes flag)
    {
        return (attributes & flag) == flag;
    }

    public static MetadataAccessibility GetAccessibilityFromAccessors(MetadataMethod? accessor1,
                                                                      MetadataMethod? accessor2)
    {
        if (accessor1 is null)
            return accessor2!.Accessibility;

        if (accessor2 is null)
            return accessor1!.Accessibility;

        return GetAccessibilityFromAccessors(accessor1.Accessibility, accessor2.Accessibility);
    }

    public static MetadataAccessibility GetAccessibilityFromAccessors(MetadataAccessibility accessibility1, MetadataAccessibility accessibility2)
    {
        var minAccessibility = (accessibility1 > accessibility2) ? accessibility2 : accessibility1;
        var maxAccessibility = (accessibility1 > accessibility2) ? accessibility1 : accessibility2;

        return (minAccessibility == MetadataAccessibility.Family) && (maxAccessibility == MetadataAccessibility.Assembly)
            ? MetadataAccessibility.FamilyOrAssembly
            : maxAccessibility;
    }

    public static VarianceKind GetVarianceKind(this GenericParameterAttributes attributes)
    {
        var variance = attributes & GenericParameterAttributes.VarianceMask;

        switch (variance)
        {
            case GenericParameterAttributes.None:
                return VarianceKind.None;
            case GenericParameterAttributes.Covariant:
                return VarianceKind.Out;
            case GenericParameterAttributes.Contravariant:
                return VarianceKind.In;
            default:
                throw new ArgumentOutOfRangeException("attributes");
        }
    }

    public static bool IsFlagSet(this GenericParameterAttributes attributes, GenericParameterAttributes flag)
    {
        return (attributes & flag) == flag;
    }

    public static bool IsFlagSet(this ParameterAttributes attributes, ParameterAttributes flag)
    {
        return (attributes & flag) == flag;
    }

    public static bool IsFlagSet(this PropertyAttributes attributes, PropertyAttributes flag)
    {
        return (attributes & flag) == flag;
    }

    public static bool IsFlagSet(this EventAttributes attributes, EventAttributes flag)
    {
        return (attributes & flag) == flag;
    }

    public static bool IsFlagSet(this AssemblyFlags flags, AssemblyFlags flag)
    {
        return (flags & flag) == flag;
    }

    public static AssemblyContentType GetContentType(this AssemblyFlags flags)
    {
        var contentType = flags & AssemblyFlags.ContentTypeMask;
        return contentType == AssemblyFlags.WindowsRuntime
            ? AssemblyContentType.WindowsRuntime
            : AssemblyContentType.Default;
    }

    public static string ToHexString(this ImmutableArray<byte> bytes)
    {
        if (bytes.IsEmpty)
            return String.Empty;

        var sb = new StringBuilder();

        foreach (var b in bytes)
            sb.Append(b.ToString("x"));

        return sb.ToString();
    }

    public static ImmutableArray<byte> ToPublicKeyToken(this ImmutableArray<byte> publicKey, AssemblyHashAlgorithm algorithm)
    {
        if (publicKey.IsEmpty)
            return ImmutableArray<byte>.Empty;

        // This could use some unsafe magic -- it's currently doing a log of copies.

        var buffer = new byte[publicKey.Length];
        publicKey.CopyTo(buffer);

        byte[] hash;
        using (var sha1 = CreateAlgorithm(algorithm))
            hash = sha1.ComputeHash(buffer);

        const int publicKeyTokenSize = 8;

        // PublicKeyToken is the low 64 bits of the hash

        var l = hash.Length - 1;
        var result = ImmutableArray.CreateBuilder<byte>(publicKeyTokenSize);
        for (var i = 0; i < publicKeyTokenSize; i++)
            result.Add(hash[l - i]);

        return result.ToImmutable();

        static HashAlgorithm CreateAlgorithm(AssemblyHashAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case AssemblyHashAlgorithm.MD5:
                    return MD5.Create();
                case AssemblyHashAlgorithm.None:
                case AssemblyHashAlgorithm.Sha1:
                    return SHA1.Create();
                case AssemblyHashAlgorithm.Sha256:
                    return SHA256.Create();
                case AssemblyHashAlgorithm.Sha384:
                    return SHA384.Create();
                case AssemblyHashAlgorithm.Sha512:
                    return SHA512.Create();
                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null);
            }
        }
    }
}