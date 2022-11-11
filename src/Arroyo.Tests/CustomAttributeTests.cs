using Arroyo.Signatures;

namespace Arroyo.Tests;

public sealed class CustomAttributeTests
{
    [Fact]
    public void CustomAttribute_Generic()
    {
        var source = @"
            class GenericAttribute<T> : System.Attribute
            {
            }

            [Generic<C>]
            class C {}
        ";

        var assembly = MetadataFactory.CreateAssembly(source);
        var c = assembly.Should().ContainSingleType("C").Subject;
        var ca = c.GetCustomAttributes().Should().ContainSingle().Subject;

        var instance = ca.Constructor.ContainingType.Should().BeAssignableTo<MetadataNamedTypeInstance>().Subject;
        instance.GenericType.GetFullName().Should().Be("GenericAttribute");
    }
}