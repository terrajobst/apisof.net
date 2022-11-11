using System.Collections.Immutable;
using Arroyo.Signatures;

namespace Arroyo;

internal static class MetadataTypeProcessor
{
    public static void Process(ref MetadataType type,
                               out bool isByRef,
                               out ImmutableArray<MetadataCustomModifier> customModifiers,
                               out ImmutableArray<MetadataCustomModifier> refCustomModifiers)
    {
        var initialCustomModifiers = ConsumeModifiers(ref type);

        if (type is not MetadataByReferenceType byRef)
        {
            isByRef = false;
            customModifiers = initialCustomModifiers;
            refCustomModifiers = ImmutableArray<MetadataCustomModifier>.Empty;
        }
        else
        {
            isByRef = true;
            customModifiers = ConsumeModifiers(ref type);
            refCustomModifiers = initialCustomModifiers;
            type = byRef.ElementType;
        }
    }

    private static ImmutableArray<MetadataCustomModifier> ConsumeModifiers(ref MetadataType type)
    {
        var customModifiers = ImmutableArray<MetadataCustomModifier>.Empty;
        var customModifierBuilder = (ImmutableArray<MetadataCustomModifier>.Builder?)null;

        while (type is MetadataModifiedType modifiedType)
        {
            customModifierBuilder ??= ImmutableArray.CreateBuilder<MetadataCustomModifier>();
            customModifierBuilder.Add(modifiedType.CustomModifier);
            type = modifiedType.UnmodifiedType;
        }

        if (customModifierBuilder is not null)
            customModifiers = customModifierBuilder.ToImmutable();

        return customModifiers;
    }

    public static MetadataType Combine(MetadataType type,
                                       bool isByRef,
                                       ImmutableArray<MetadataCustomModifier> customModifiers,
                                       ImmutableArray<MetadataCustomModifier> refCustomModifiers)
    {
        if (isByRef)
        {
            CombineModifiers(ref type, refCustomModifiers);
            type = new MetadataByReferenceType(type);
        }

        CombineModifiers(ref type, customModifiers);
        return type;
    }

    private static void CombineModifiers(ref MetadataType type, ImmutableArray<MetadataCustomModifier> customModifiers)
    {
        for (var i = 0; i < customModifiers.Length; i++)
        {
            var modifier = customModifiers[i];
            type = new MetadataModifiedType(type, modifier);
        }
    }
}