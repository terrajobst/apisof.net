using System.Diagnostics;
using FluentAssertions.Collections;

namespace Arroyo.Tests.Infra;

[DebuggerNonUserCode]
internal static class MetadataAssertionExtensions 
{
    public static MetadataAssertions<T> Should<T>(this T instance)
        where T: class?, IMetadataItem?
    {
        return new MetadataAssertions<T>(instance); 
    }

    public static MetadataAssemblyAssertions Should(this MetadataAssembly instance)
    {
        return new MetadataAssemblyAssertions(instance); 
    } 
    
    public static MetadataCustomAttributeAssertions Should(this MetadataCustomAttribute instance)
    {
        return new MetadataCustomAttributeAssertions(instance); 
    }
    
    [CustomAssertion]
    public static AndWhichConstraint<GenericCollectionAssertions<MetadataCustomAttribute>, MetadataCustomAttribute> ContainSingleAttribute(this GenericCollectionAssertions<MetadataCustomAttribute> instance, string typeName)
    {
        return instance.ContainSingle(ca => IsOfType(ca, typeName));
    }

    private static bool IsOfType(MetadataCustomAttribute ca, string typeName)
    {
        return ca.Constructor.ContainingType is MetadataNamedTypeReference att &&
               att.GetFullName() == typeName;
    }
    
    [CustomAssertion]
    public static AndWhichConstraint<MetadataAssertions<MetadataNamedType>, MetadataCustomAttribute> ContainSingleAttribute(this MetadataAssertions<MetadataNamedType> assertions, string typeName)
    {
        var ca = assertions.Subject.GetCustomAttributes(true).Should().ContainSingleAttribute(typeName).Subject;
        return new AndWhichConstraint<MetadataAssertions<MetadataNamedType>, MetadataCustomAttribute>(assertions, ca);
    }

    [CustomAssertion]
    public static AndWhichConstraint<MetadataAssertions<MetadataField>, MetadataCustomAttribute> ContainSingleAttribute(this MetadataAssertions<MetadataField> assertions, string typeName)
    {
        var ca = assertions.Subject.GetCustomAttributes(true).Should().ContainSingleAttribute(typeName).Subject;
        return new AndWhichConstraint<MetadataAssertions<MetadataField>, MetadataCustomAttribute>(assertions, ca);
    }

    [CustomAssertion]
    public static AndWhichConstraint<MetadataAssertions<MetadataMethod>, MetadataCustomAttribute> ContainSingleAttribute(this MetadataAssertions<MetadataMethod> assertions, string typeName)
    {
        var ca = assertions.Subject.GetCustomAttributes(true).Should().ContainSingleAttribute(typeName).Subject;
        return new AndWhichConstraint<MetadataAssertions<MetadataMethod>, MetadataCustomAttribute>(assertions, ca);
    }

    [CustomAssertion]
    public static AndWhichConstraint<MetadataAssertions<MetadataProperty>, MetadataCustomAttribute> ContainSingleAttribute(this MetadataAssertions<MetadataProperty> assertions, string typeName)
    {
        var ca = assertions.Subject.GetCustomAttributes(true).Should().ContainSingleAttribute(typeName).Subject;
        return new AndWhichConstraint<MetadataAssertions<MetadataProperty>, MetadataCustomAttribute>(assertions, ca);
    }
    
    [CustomAssertion]
    public static AndWhichConstraint<MetadataAssertions<MetadataEvent>, MetadataCustomAttribute> ContainSingleAttribute(this MetadataAssertions<MetadataEvent> assertions, string typeName)
    {
        var ca = assertions.Subject.GetCustomAttributes(true).Should().ContainSingleAttribute(typeName).Subject;
        return new AndWhichConstraint<MetadataAssertions<MetadataEvent>, MetadataCustomAttribute>(assertions, ca);
    }
    
    [CustomAssertion]
    public static AndWhichConstraint<MetadataAssertions<MetadataParameter>, MetadataCustomAttribute> ContainSingleAttribute(this MetadataAssertions<MetadataParameter> assertions, string typeName)
    {
        var ca = assertions.Subject.GetCustomAttributes(true).Should().ContainSingleAttribute(typeName).Subject;
        return new AndWhichConstraint<MetadataAssertions<MetadataParameter>, MetadataCustomAttribute>(assertions, ca);
    }
}