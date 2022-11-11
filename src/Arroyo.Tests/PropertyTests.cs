namespace Arroyo.Tests;

public sealed class PropertyTests
{
    [Fact]
    public void Property_ContainingType()
    {
        var source = @"
            class C {
                int P { get; set; }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var c = a.Should().ContainSingleType("C").Subject;
        var p = a.Should().ContainSingleProperty("C.P").Subject;

        p.ContainingType.Should().BeSameAs(c);
    }
    
    [Fact]
    public void Property_PropertyType()
    {
        var source = @"
            class C {
                int P { get; set; }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var p = a.Should().ContainSingleProperty("C.P").Subject;

        var t = p.PropertyType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        t.GetFullName().Should().Be("System.Int32");
    }
    
    [Fact]
    public void Property_PropertyType_ref()
    {
        var source = @"
            struct S {
                ref int P1 { get => throw null; }
                    int P2 { get => throw null; }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var p1 = a.Should().ContainSingleProperty("S.P1").Subject;
        var p2 = a.Should().ContainSingleProperty("S.P2").Subject;

        p1.GetCustomAttributes().Should().BeEmpty();
        p1.CustomModifiers.Should().BeEmpty();
        p1.RefCustomModifiers.Should().BeEmpty();
        p1.RefKind.Should().Be(MetadataRefKind.Ref);
        var p1t = p1.PropertyType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        p1t.GetFullName().Should().Be("System.Int32");
        
        p2.GetCustomAttributes().Should().BeEmpty();
        p2.CustomModifiers.Should().BeEmpty();
        p2.RefCustomModifiers.Should().BeEmpty();
        p2.RefKind.Should().Be(MetadataRefKind.None);
        var p2t = p2.PropertyType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        p2t.GetFullName().Should().Be("System.Int32");
    }

    [Fact]
    public void Property_PropertyType_ref_readonly()
    {
        var source = @"
            struct S {
                ref readonly int P { get => throw null; }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var p = a.Should().ContainSingleProperty("S.P").Subject;

        p.GetCustomAttributes().Should().BeEmpty();
        p.Should()
         .ContainSingleAttribute("System.Runtime.CompilerServices.IsReadOnlyAttribute");
        p.CustomModifiers.Should().BeEmpty();
        
        var pm = p.RefCustomModifiers.Should().ContainSingle().Subject;
        pm.IsRequired.Should().BeTrue();
        var pmt = pm.Type.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        pmt.GetFullName().Should().Be("System.Runtime.InteropServices.InAttribute");
        
        p.RefKind.Should().Be(MetadataRefKind.In);
        var p1t = p.PropertyType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        p1t.GetFullName().Should().Be("System.Int32");
    }
    
    [Fact]
    public void Property_AssociatedMember_Getter_Accessors()
    {
        var source = @"
            class C {
                int P {
                    get { throw null; }
                }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var p = a.Should().ContainSingleProperty("C.P").Subject;
        var g = a.Should().ContainSingleMethod("C.get_P").Subject;

        p.Getter.Should().BeSameAs(g);
        p.Setter.Should().BeNull();
        g.AssociatedMember.Should().BeSameAs(p);
        p.Getter!.Kind.Should().Be(MethodKind.PropertyGetter);
        p.Accessors.Should().Equal(p.Getter);
    }

    [Fact]
    public void Property_AssociatedMember_Setter_Accessors()
    {
        var source = @"
            class C {
                int P {
                    set { }
                }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var p = a.Should().ContainSingleProperty("C.P").Subject;
        var s = a.Should().ContainSingleMethod("C.set_P").Subject;

        p.Setter.Should().BeSameAs(s);
        p.Getter.Should().BeNull();
        s.AssociatedMember.Should().BeSameAs(p);
        p.Setter!.Kind.Should().Be(MethodKind.PropertySetter);
        p.Accessors.Should().Equal(p.Setter);
    }

    [Fact]
    public void Property_AssociatedMember_Getter_Setter_Accessors()
    {
        var source = @"
            class C {
                int P {
                    get { throw null; }
                    set { }
                }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var p = a.Should().ContainSingleProperty("C.P").Subject;
        var g = a.Should().ContainSingleMethod("C.get_P").Subject;
        var s = a.Should().ContainSingleMethod("C.set_P").Subject;

        p.Setter.Should().BeSameAs(s);
        s.AssociatedMember.Should().BeSameAs(p);
        p.Setter!.Kind.Should().Be(MethodKind.PropertySetter);

        p.Getter.Should().BeSameAs(g);
        g.AssociatedMember.Should().BeSameAs(p);
        p.Getter!.Kind.Should().Be(MethodKind.PropertyGetter);
        p.Accessors.Should().Equal(p.Getter, p.Setter);
    }
    
    [Fact]
    public void Property_Indexer()
    {
        var source = @"
            class C {
                int P { get => throw null; }
                int this[int x] { get => throw null; }
                int this[int x, float y] { get => throw null; }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var c = a.Should().ContainSingleType("C").Subject;

        var properties = c.GetProperties();
        
        properties.Should().SatisfyRespectively(
            p =>
            {
                p.Name.Should().Be("P");
                p.Parameters.Should().BeEmpty();
            },
            p =>
            {
                p.Name.Should().Be("Item");
                var x = p.Parameters.Should().ContainSingle().Subject;
                x.Name.Should().Be("x");
                x.ParameterType.GetDocumentationId().Should().Be("T:System.Int32");
            },
            p =>
            {
                p.Name.Should().Be("Item");
                p.Parameters.Should().SatisfyRespectively(
                    x =>
                    {
                        x.Name.Should().Be("x");
                        x.ParameterType.GetDocumentationId().Should().Be("T:System.Int32");
                    },
                    y =>
                    {
                        y.Name.Should().Be("y");
                        y.ParameterType.GetDocumentationId().Should().Be("T:System.Single");
                    }
                );
            }
        );
    }

    [Fact]
    public void Property_Indexer_Named()
    {
        var source = @"
            using System.Runtime.CompilerServices;
            class C {
                [IndexerName(""Chars"")]
                int this[int x] { get => throw null; }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var c = a.Should().ContainSingleType("C").Subject;
        var p = c.GetProperties().Should().ContainSingle().Subject;

        p.Name.Should().Be("Chars");
        var x = p.Parameters.Should().ContainSingle().Subject;
        x.Name.Should().Be("x");
        x.ParameterType.GetDocumentationId().Should().Be("T:System.Int32");
    }

    [Theory]
    [TestData(nameof(TestData.Accessibility))]
    public void Property_Accessibility(string modifier, MetadataAccessibility expectedAccessibility)
    {
        var source = @$"
            class C {{
                {modifier} int P {{ get; set; }}
            }}
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var p = a.Should().ContainSingleProperty("C.P").Subject;
        
        p.Accessibility.Should().Be(expectedAccessibility);
    }

    // TODO: Add coverage for setters/getters having disjoint accessibility

    [Fact]
    public void Property_required()
    {
        var source = @"
            class C {
                public required int P1 { get; set; }
                public          int P2 { get; set; }
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
        ctor.GetCustomAttributes().Should().BeEmpty();
        ctor.Should()
            .ContainSingleAttribute("System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute")
            .Which
            .Should().HaveFixedArguments(
                feature => feature.Value.Should().Be("RequiredMembers"))
            .And
            .HaveNoNamedArguments();
        ctor.Should()
            .ContainSingleAttribute("System.ObsoleteAttribute")
            .Which
            .Should().HaveFixedArguments(
                message => message.Value.Should().Be("Constructors of types with required members are not supported in this version of your compiler."),
                isError => isError.Value.Should().Be(true))
            .And
            .HaveNoNamedArguments();

        var p1 = a.Should().ContainSingleProperty("C.P1").Subject;
        p1.IsRequired.Should().BeTrue();
        p1.GetCustomAttributes().Should().BeEmpty();
        p1.Should()
          .ContainSingleAttribute("System.Runtime.CompilerServices.RequiredMemberAttribute");

        var p2 = a.Should().ContainSingleProperty("C.P2").Subject;
        p2.IsRequired.Should().BeFalse();
        p2.GetCustomAttributes().Should().BeEmpty();
    }

    [Fact]
    public void Property_CustomAttribute()
    {
        var source = @"
            class C {
                [D]
                int P { get; set; }
            }
            class D : System.Attribute {}            
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var p = a.Should().ContainSingleProperty("C.P").Subject;

        var ca = p.GetCustomAttributes().Single();
        ca.Constructor.ContainingType.GetDocumentationId().Should().Be("T:D");
    }
}