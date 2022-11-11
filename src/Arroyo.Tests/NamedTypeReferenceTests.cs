using Arroyo.Signatures;

namespace Arroyo.Tests;

public sealed class NamedTypeReferenceTests
{
    [Fact]
    public void NamedTypeReference_GenericNesting_G_G()
    {
        var source = @"
            class Outer<K> {
                public class Inner<T> {
                }
            }
            static class C {                   
               static void M(Outer<int>.Inner<byte> a) {}
            }
        ";

        var assembly = MetadataFactory.CreateAssembly(source);
        var c = assembly.Should().ContainSingleType("C").Subject;
        var m = c.GetMethods().Should().ContainSingle().Subject;
        var t = m.Parameters.Should().ContainSingle().Subject.ParameterType;

        var innerInstance = t.Should().BeAssignableTo<MetadataNamedTypeInstance>().Subject;
        var innerArgument = innerInstance.TypeArguments.Should().ContainSingle().Subject; 

        var byteType = innerArgument.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        byteType.GetFullName().Should().Be("System.Byte");
        byteType.GenericArity.Should().Be(0);

        var innerType = innerInstance.GenericType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        innerType.Name.Should().Be("Inner");
        innerType.GenericArity.Should().Be(1);

        var outerInstance = innerInstance.ContainingType.Should().BeAssignableTo<MetadataNamedTypeInstance>().Subject;
        var outerArgument = outerInstance.TypeArguments.Should().ContainSingle().Subject; 

        var int32Type = outerArgument.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        int32Type.GetFullName().Should().Be("System.Int32");
        int32Type.GenericArity.Should().Be(0);
        int32Type.ContainingType.Should().BeNull();

        var outerType = outerInstance.GenericType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        outerType.Name.Should().Be("Outer");
        outerType.GenericArity.Should().Be(1);
        outerType.ContainingType.Should().BeNull();
    }
    
    [Fact]
    public void NamedTypeReference_GenericNesting_N_G()
    {
        var source = @"
            class Outer {
                public class Inner<T> {
                }
            }
            static class C {                   
               static void M(Outer.Inner<byte> a) {}
            }
        ";

        var assembly = MetadataFactory.CreateAssembly(source);
        var c = assembly.Should().ContainSingleType("C").Subject;
        var m = c.GetMethods().Should().ContainSingle().Subject;
        var t = m.Parameters.Should().ContainSingle().Subject.ParameterType;

        var innerInstance = t.Should().BeAssignableTo<MetadataNamedTypeInstance>().Subject;
        var innerArgument = innerInstance.TypeArguments.Should().ContainSingle().Subject; 

        var byteType = innerArgument.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        byteType.GetFullName().Should().Be("System.Byte");
        byteType.GenericArity.Should().Be(0);

        var innerType = innerInstance.GenericType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        innerType.Name.Should().Be("Inner");
        innerType.GenericArity.Should().Be(1);

        var outerType = innerInstance.ContainingType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        outerType.Name.Should().Be("Outer");
        outerType.GenericArity.Should().Be(0);
        outerType.ContainingType.Should().BeNull();
    }
    
    [Fact]
    public void NamedTypeReference_GenericNesting_G_N()
    {
        var source = @"
            class Outer<K> {
                public class Inner {
                }
            }
            static class C {                   
               static void M(Outer<int>.Inner a) {}
            }
        ";

        var assembly = MetadataFactory.CreateAssembly(source);
        var c = assembly.Should().ContainSingleType("C").Subject;
        var m = c.GetMethods().Should().ContainSingle().Subject;
        var t = m.Parameters.Should().ContainSingle().Subject.ParameterType;

        var innerType = t.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        innerType.Name.Should().Be("Inner");
        innerType.GenericArity.Should().Be(0);

        var outerInstance = innerType.ContainingType.Should().BeAssignableTo<MetadataNamedTypeInstance>().Subject;
        var outerArgument = outerInstance.TypeArguments.Should().ContainSingle().Subject; 

        var int32Type = outerArgument.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        int32Type.GetFullName().Should().Be("System.Int32");
        int32Type.GenericArity.Should().Be(0);
        int32Type.ContainingType.Should().BeNull();

        var outerType = outerInstance.GenericType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        outerType.Name.Should().Be("Outer");
        outerType.GenericArity.Should().Be(1);
        outerType.ContainingType.Should().BeNull();
    }
    
    [Fact]
    public void NamedTypeReference_GenericNesting_G_N_G()
    {
        var source = @"
            class Outer<K> {
                public class Middle {
                    public class Inner<T> {
                    }
                }
            }
            static class C {                   
               static void M(Outer<int>.Middle.Inner<byte> a) {}
            }
        ";

        var assembly = MetadataFactory.CreateAssembly(source);
        var c = assembly.Should().ContainSingleType("C").Subject;
        var m = c.GetMethods().Should().ContainSingle().Subject;
        var t = m.Parameters.Should().ContainSingle().Subject.ParameterType;

        var innerInstance = t.Should().BeAssignableTo<MetadataNamedTypeInstance>().Subject;
        var innerArgument = innerInstance.TypeArguments.Should().ContainSingle().Subject; 

        var byteType = innerArgument.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        byteType.GetFullName().Should().Be("System.Byte");
        byteType.GenericArity.Should().Be(0);

        var innerType = innerInstance.GenericType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        innerType.Name.Should().Be("Inner");
        innerType.GenericArity.Should().Be(1);
        
        var middleInstance = innerInstance.ContainingType.Should().BeAssignableTo<MetadataNamedTypeInstance>().Subject;
        middleInstance.TypeArguments.Should().BeEmpty();

        var middleType = middleInstance.GenericType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        middleType.Name.Should().Be("Middle");
        middleType.GenericArity.Should().Be(0);

        var outerInstance = middleInstance.ContainingType.Should().BeAssignableTo<MetadataNamedTypeInstance>().Subject;
        var outerArgument = outerInstance.TypeArguments.Should().ContainSingle().Subject; 

        var int32Type = outerArgument.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        int32Type.GetFullName().Should().Be("System.Int32");
        int32Type.GenericArity.Should().Be(0);
        int32Type.ContainingType.Should().BeNull();

        var outerType = outerInstance.GenericType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        outerType.Name.Should().Be("Outer");
        outerType.GenericArity.Should().Be(1);
        outerType.ContainingType.Should().BeNull();
    }
    
        [Fact]
    public void NamedTypeReference_GenericNesting_N_G_N()
    {
        var source = @"
            class Outer {
                public class Middle<T> {
                    public class Inner {
                    }
                }
            }
            static class C {                   
               static void M(Outer.Middle<int>.Inner a) {}
            }
        ";

        var assembly = MetadataFactory.CreateAssembly(source);
        var c = assembly.Should().ContainSingleType("C").Subject;
        var m = c.GetMethods().Should().ContainSingle().Subject;
        var t = m.Parameters.Should().ContainSingle().Subject.ParameterType;

        var innerInstance = t.Should().BeAssignableTo<MetadataNamedTypeInstance>().Subject;
        innerInstance.TypeArguments.Should().BeEmpty();

        var innerType = innerInstance.GenericType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        innerType.Name.Should().Be("Inner");
        innerType.GenericArity.Should().Be(0);
        
        var middleInstance = innerInstance.ContainingType.Should().BeAssignableTo<MetadataNamedTypeInstance>().Subject;
        var middleArgument = middleInstance.TypeArguments.Should().ContainSingle().Subject; 

        var int32Type = middleArgument.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        int32Type.GetFullName().Should().Be("System.Int32");
        int32Type.GenericArity.Should().Be(0);
        int32Type.ContainingType.Should().BeNull();

        var middleType = middleInstance.GenericType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        middleType.Name.Should().Be("Middle");
        middleType.GenericArity.Should().Be(1);

        var outerType = middleInstance.ContainingType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        outerType.Name.Should().Be("Outer");
        outerType.GenericArity.Should().Be(0);
        outerType.ContainingType.Should().BeNull();
    }
}