using Arroyo.Signatures;

namespace Arroyo.Tests;

public sealed class StructTests
{
    [Fact]
    public void Struct_DefaultCtor_Undefined()
    {
        var source = @"
            struct S {}
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var s = a.Should().ContainSingleType("S").Subject;
        
        s.GetMethods().Should().BeEmpty();
    }
    
    [Fact]
    public void Struct_DefaultCtor_Defined()
    {
        var source = @"
            struct S {
                public S() {}
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
       
        a.Should().ContainSingleMethod("S.#ctor");
    }
    
    [Fact]
    public void Struct_ref()
    {
        var source = @"
            ref struct S1 {
            }
            struct S2 {
            }
            namespace System.Runtime.CompilerServices
            {
                public sealed class RequiredMemberAttribute : System.Attribute { }
                public sealed class CompilerFeatureRequiredAttribute : System.Attribute
                {
                    public CompilerFeatureRequiredAttribute(string featureName) {}
                }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        
        var s1 = a.Should().ContainSingleType("S1").Subject;
        s1.IsRefLikeType.Should().BeTrue();
        s1.GetCustomAttributes().Should().BeEmpty();
        s1.Should()
          .ContainSingleAttribute("System.Runtime.CompilerServices.IsByRefLikeAttribute");
        s1.Should()
          .ContainSingleAttribute("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute")
          .Which
          .Should().HaveFixedArguments(
              feature => feature.Value.Should().Be("RefStructs"))
          .And
          .HaveNoNamedArguments();

        s1.Should()
          .ContainSingleAttribute("System.ObsoleteAttribute")
          .Which
          .Should().HaveFixedArguments(
              message => message.Value.Should().Be("Types with embedded references are not supported in this version of your compiler."),
              isError => isError.Value.Should().Be(true))
          .And
          .HaveNoNamedArguments();

        var s2 = a.Should().ContainSingleType("S2").Subject;
        s2.IsRefLikeType.Should().BeFalse();
        s2.GetCustomAttributes().Should().BeEmpty();
        s2.GetCustomAttributes(includeProcessed: true).Should().BeEmpty();
    }
    
    [Fact]
    public void Struct_ref_RequiredFeature()
    {
        var source = @"
            ref struct S1 {
            }
            struct S2 {
            }
            namespace System.Runtime.CompilerServices
            {
                public sealed class CompilerFeatureRequiredAttribute : System.Attribute
                {
                    public CompilerFeatureRequiredAttribute(string featureName) {}
                }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);

        var s1 = a.Should().ContainSingleType("S1").Subject;
        s1.IsRefLikeType.Should().BeTrue();
        s1.GetCustomAttributes().Should().BeEmpty();

        var s2 = a.Should().ContainSingleType("S2").Subject;
        s2.IsRefLikeType.Should().BeFalse();
        s2.GetCustomAttributes().Should().BeEmpty();
    }
    
    [Fact]
    public void Struct_readonly()
    {
        var source = @"
            readonly struct S1 {
            }
                     struct S2 {
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);

        var s1 = a.Should().ContainSingleType("S1").Subject;
        s1.IsReadOnly.Should().BeTrue();
        s1.GetCustomAttributes().Should().BeEmpty();
        s1.Should()
            .ContainSingleAttribute("System.Runtime.CompilerServices.IsReadOnlyAttribute");

        var s2 = a.Should().ContainSingleType("S2").Subject;
        s2.IsReadOnly.Should().BeFalse();
        s2.GetCustomAttributes().Should().BeEmpty();
        s2.GetCustomAttributes(includeProcessed: true).Should().BeEmpty();
    }

    [Fact]
    public void Struct_readonly_Method()
    {
        var source = @"
            struct S {
                readonly void M1() { }
                         void M2() { }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);

        var m1 = a.Should().ContainSingleMethod("S.M1").Subject;
        m1.IsReadOnly.Should().BeTrue();
        m1.GetCustomAttributes().Should().BeEmpty();
        m1.Should()
          .ContainSingleAttribute("System.Runtime.CompilerServices.IsReadOnlyAttribute");
        
        var m2 = a.Should().ContainSingleMethod("S.M2").Subject;
        m2.IsReadOnly.Should().BeFalse();
        m2.GetCustomAttributes().Should().BeEmpty();
        m2.GetCustomAttributes(includeProcessed: true).Should().BeEmpty();
    }
    
    [Fact]
    public void Struct_readonly_Property()
    {
        var source = @"
            struct S {
                readonly int P1
                {
                    get => throw null;
                }
                readonly int P2
                {
                    get => throw null;
                    set => throw null;
                }
                int P3
                {
                    readonly get => throw null;
                    set => throw null;
                }
                int P4
                {
                    get => throw null;
                    readonly set => throw null;
                }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var p1 = a.Should().ContainSingleProperty("S.P1").Subject;
        var p2 = a.Should().ContainSingleProperty("S.P2").Subject;
        var p3 = a.Should().ContainSingleProperty("S.P3").Subject;
        var p4 = a.Should().ContainSingleProperty("S.P4").Subject;
        
        p1.GetCustomAttributes().Should().BeEmpty();
        AssertMethodReadOnly(p1.Getter);
        p1.Setter.Should().BeNull();

        p2.GetCustomAttributes().Should().BeEmpty();
        AssertMethodReadOnly(p2.Getter);
        AssertMethodReadOnly(p2.Setter);

        p3.GetCustomAttributes().Should().BeEmpty();
        AssertMethodReadOnly(p3.Getter);
        AssertMethodNotReadOnly(p3.Setter);

        p4.GetCustomAttributes().Should().BeEmpty();
        AssertMethodNotReadOnly(p4.Getter);
        AssertMethodReadOnly(p4.Setter);

        static void AssertMethodReadOnly(MetadataMethod? m)
        {
            m.Should().NotBeNull();
            m!.IsReadOnly.Should().BeTrue();
            m.GetCustomAttributes().Should().BeEmpty();
            m.Should()
             .ContainSingleAttribute("System.Runtime.CompilerServices.IsReadOnlyAttribute");
        }

        static void AssertMethodNotReadOnly(MetadataMethod? m)
        {
            m.Should().NotBeNull();
            m!.IsReadOnly.Should().BeFalse();
            m.GetCustomAttributes().Should().BeEmpty();
            m.GetCustomAttributes(includeProcessed: true).Should().BeEmpty();
        }
    }

    [Fact]
    public void Struct_FixedSizeBuffer()
    {
        var source = @"
            unsafe struct S
            {
                private fixed byte F[10];
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var s = a.Should().ContainSingleType("S").Subject;
        var f = s.GetFields().Should().ContainSingle().Subject;
        f.FixedSizeBuffer.Should().Be(10);
        f.GetCustomAttributes().Should().BeEmpty();
        f.Should()
         .ContainSingleAttribute("System.Runtime.CompilerServices.FixedBufferAttribute")
         .Which
         .Should().HaveFixedArguments(
             feature => feature.Value.Should().BeAssignableTo<MetadataNamedTypeReference>()
                 .Which.GetFullName().Should().Be("System.Byte"),
             feature => feature.Value.Should().Be(10))
         .And
         .HaveNoNamedArguments();

        var ftp = f.FieldType.Should().BeAssignableTo<MetadataPointerType>().Subject;
        var ftpe = ftp.ElementType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        ftpe.GetFullName().Should().Be("System.Byte");
    }
}
