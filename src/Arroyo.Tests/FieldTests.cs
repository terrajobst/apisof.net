namespace Arroyo.Tests;

public sealed class FieldTests
{
    [Fact]
    public void Field_ContainingType()
    {
        var source = @"
            class C {
                int F;
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var c = a.Should().ContainSingleType("C").Subject;
        var f = a.Should().ContainSingleField("C.F").Subject;

        f.ContainingType.Should().BeSameAs(c);
    }
    
    [Fact]
    public void Field_FieldType()
    {
        var source = @"
            class C {
                int F;
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var f = a.Should().ContainSingleField("C.F").Subject;

        var t = f.FieldType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        t.GetFullName().Should().Be("System.Int32");
    }

    [Fact]
    public void Field_FieldType_ref()
    {
        var source = @"
            struct S {
                ref int F1;
                    int F2;
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var f1 = a.Should().ContainSingleField("S.F1").Subject;
        var f2 = a.Should().ContainSingleField("S.F2").Subject;

        f1.GetCustomAttributes().Should().BeEmpty();
        f1.CustomModifiers.Should().BeEmpty();
        f1.RefCustomModifiers.Should().BeEmpty();
        f1.RefKind.Should().Be(MetadataRefKind.Ref);
        var f1t = f1.FieldType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        f1t.GetFullName().Should().Be("System.Int32");
        
        f2.GetCustomAttributes().Should().BeEmpty();
        f2.CustomModifiers.Should().BeEmpty();
        f2.RefCustomModifiers.Should().BeEmpty();
        f2.RefKind.Should().Be(MetadataRefKind.None);
        var f2t = f2.FieldType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        f2t.GetFullName().Should().Be("System.Int32");
    }

    [Fact]
    public void Field_FieldType_ref_readonly()
    {
        var source = @"
            struct S {
                ref readonly int F;
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var f = a.Should().ContainSingleField("S.F").Subject;

        f.GetCustomAttributes().Should().BeEmpty();
        f.CustomModifiers.Should().BeEmpty();
        f.RefCustomModifiers.Should().BeEmpty();
        f.RefKind.Should().Be(MetadataRefKind.In);
        var f1t = f.FieldType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        f1t.GetFullName().Should().Be("System.Int32");
    }

    [Fact]
    public void Field_FieldType_Volatile()
    {
        var source = @"
            class C {
                volatile int F1;
                         int F2;
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var f1 = a.Should().ContainSingleField("C.F1").Subject;
        var f2 = a.Should().ContainSingleField("C.F2").Subject;

        f1.IsVolatile.Should().BeTrue();
        var f1t = f1.FieldType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        f1t.GetFullName().Should().Be("System.Int32");
        var f1m = f1.CustomModifiers.Should().ContainSingle().Subject;
        f1m.IsRequired.Should().BeTrue();
        var f1mt = f1m.Type.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        f1mt.GetFullName().Should().Be("System.Runtime.CompilerServices.IsVolatile");
        f1.RefCustomModifiers.Should().BeEmpty();
        
        f2.IsVolatile.Should().BeFalse();
        var f2t = f2.FieldType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        f2t.GetFullName().Should().Be("System.Int32");
        f2.CustomModifiers.Should().BeEmpty();
        f2.RefCustomModifiers.Should().BeEmpty();
    }
    
    // TODO: IsPrivateScope

    [Fact]
    public void Field_IsStatic()
    {
        var source = @"
            class C {
                static int F1;
                       int F2;
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var f1 = a.Should().ContainSingleField("C.F1").Subject;
        var f2 = a.Should().ContainSingleField("C.F2").Subject;
        
        f1.IsStatic.Should().BeTrue();
        f2.IsStatic.Should().BeFalse();
    }
    
    [Fact]
    public void Field_IsInitOnly()
    {
        var source = @"
            class C {
                readonly int F1;
                         int F2;
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var f1 = a.Should().ContainSingleField("C.F1").Subject;
        var f2 = a.Should().ContainSingleField("C.F2").Subject;

        f1.IsInitOnly.Should().BeTrue();
        f2.IsInitOnly.Should().BeFalse();
    }
    
    [Fact]
    public void Field_IsLiteral()
    {
        var source = @"
            class C {
                const int F1 = 0;
                      int F2;
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var f1 = a.Should().ContainSingleField("C.F1").Subject;
        var f2 = a.Should().ContainSingleField("C.F2").Subject;

        f1.IsLiteral.Should().BeTrue();
        f2.IsLiteral.Should().BeFalse();
    }

    [Fact]
    public void Field_IsNotSerialized()
    {
        // TODO(RAISING): Pseudo attribute [NonSerialized]
        
        var source = @"
            class C {
                [System.NonSerialized] int F1;
                                       int F2;
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var f1 = a.Should().ContainSingleField("C.F1").Subject;
        var f2 = a.Should().ContainSingleField("C.F2").Subject;

        f1.IsNotSerialized.Should().BeTrue();
        f2.IsNotSerialized.Should().BeFalse();
    }

    // TODO: IsSpecialName
    // TODO: IsPinvokeImplementation
    // TODO: IsRuntimeSpecialName
    // TODO: HasMarshallingInformation
    // TODO: HasFieldRva
    
    [Fact]
    public void Field_DefaultValue_HasDefault()
    {
        var source = @"
            class C {
                const int F1 = 10;
                      int F2;
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var f1 = a.Should().ContainSingleField("C.F1").Subject;
        var f2 = a.Should().ContainSingleField("C.F2").Subject;
        
        f1.DefaultValue.Should().NotBeNull();
        f1.HasDefault.Should().BeTrue();
        f1.DefaultValue!.Value.Should().Be(10);
        f2.DefaultValue.Should().BeNull();
        f2.HasDefault.Should().BeFalse();
    }
    
    [Theory]
    [TestData(nameof(TestData.Accessibility))]
    public void Field_Accessibility(string modifier, MetadataAccessibility expectedAccessibility)
    {
        var source = @$"
            class C {{
                {modifier} int F = 10;
            }}
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var f = a.Should().ContainSingleField("C.F").Subject;
        
        f.Accessibility.Should().Be(expectedAccessibility);
    }
    
    [Fact]
    public void Field_required()
    {
        var source = @"
            class C {
                public required int F1;
                                int F2;
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
        var ctor = a.Should().ContainSingleMethod("C.#ctor").Subject;
        var f1 = a.Should().ContainSingleField("C.F1").Subject;
        var f2 = a.Should().ContainSingleField("C.F2").Subject;
        
        ctor.GetCustomAttributes().Should().BeEmpty();
        ctor.Should()
            .ContainSingleAttribute("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute")
            .Which
            .Should()
            .HaveFixedArguments(
                feature => feature.Value.Should().Be("RequiredMembers"))
            .And
            .HaveNoNamedArguments();
        ctor.Should()
            .ContainSingleAttribute("System.ObsoleteAttribute")
            .Which
            .Should()
            .HaveFixedArguments(
                message => message.Value.Should().Be("Constructors of types with required members are not supported in this version of your compiler."),
                isError => isError.Value.Should().Be(true)
            )
            .And
            .HaveNoNamedArguments();
        
        f1.IsRequired.Should().BeTrue();
        f1.GetCustomAttributes().Should().BeEmpty();
        f1.GetCustomAttributes().Should().BeEmpty();
        f1.Should()
          .ContainSingleAttribute("System.Runtime.CompilerServices.RequiredMemberAttribute");
        
        f2.IsRequired.Should().BeFalse();
        f2.GetCustomAttributes().Should().BeEmpty();
    }
    
    [Fact]
    public void Field_CustomAttribute()
    {
        var source = @"
            class C {
                [D]
                int F = 10;
            }
            class D : System.Attribute {}            
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var f = a.Should().ContainSingleField("C.F").Subject;

        var ca = f.GetCustomAttributes().Single();
        ca.Constructor.ContainingType.GetDocumentationId().Should().Be("T:D");
    }
}