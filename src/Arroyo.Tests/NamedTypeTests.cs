namespace Arroyo.Tests;

public sealed class NamedTypeTests
{
    [Fact]
    public void NamedType_Class()
    {
        var source = @"
            namespace Foo;

            class Bar { }
        ";

        var assembly = MetadataFactory.CreateAssembly(source);
        var bar = assembly.Should().ContainSingleType("Foo.Bar").Subject;

        bar.Kind.Should().Be(TypeKind.Class);
        bar.IsClass.Should().BeTrue();
        bar.IsAbstract.Should().BeFalse();
        bar.IsSealed.Should().BeFalse();
        bar.IsInterface.Should().BeFalse();
    }

    [Fact]
    public void NamedType_IsSerializable()
    {
        // TODO(RAISING): Pseudo attribute [Serializable]

        var source = @"
            [System.Serializable]
            class C { }
        ";

        var assembly = MetadataFactory.CreateAssembly(source);
        var c = assembly.Should().ContainSingleType("C").Subject;

        c.IsSerializable.Should().BeTrue();
        c.GetCustomAttributes().Should().BeEmpty();
    }

    [Fact]
    public void NamedType_Generic_Nesting_N_N()
    {
        var source = @"
            class Outer {
                class Inner {
                }
            }
        ";

        var assembly = MetadataFactory.CreateAssembly(source);
        var type = assembly.Should().ContainSingleType("Outer.Inner").Subject;

        var inner = type.Should().BeAssignableTo<MetadataNamedType>().Subject;
        inner.Name.Should().Be("Inner");
        inner.GenericArity.Should().Be(0);
        inner.GenericParameters.Should().BeEmpty();

        var outer = inner.ContainingType.Should().BeAssignableTo<MetadataNamedType>().Subject;
        outer.Name.Should().Be("Outer");
        outer.GenericArity.Should().Be(0);
        outer.GenericParameters.Should().BeEmpty();
        outer.ContainingType.Should().BeNull();
    }

    [Fact]
    public void NamedType_Generic_Nesting_N_G()
    {
        var source = @"
            class Outer {
                class Inner<T> {
                }
            }
        ";

        var assembly = MetadataFactory.CreateAssembly(source);
        var type = assembly.Should().ContainSingleType("Outer.Inner").Subject;

        var inner = type.Should().BeAssignableTo<MetadataNamedType>().Subject;
        inner.Name.Should().Be("Inner");
        inner.GenericArity.Should().Be(1);
        inner.GenericParameters.Should().ContainSingle().Subject.Name.Should().Be("T");

        var outer = inner.ContainingType.Should().BeAssignableTo<MetadataNamedType>().Subject;
        outer.Name.Should().Be("Outer");
        outer.GenericArity.Should().Be(0);
        outer.GenericParameters.Should().BeEmpty();
        outer.ContainingType.Should().BeNull();
    }

    [Fact]
    public void NamedType_Generic_Nesting_G_N()
    {
        var source = @"
            class Outer<T> {
                class Inner {
                }
            }
        ";

        var assembly = MetadataFactory.CreateAssembly(source);
        var type = assembly.Should().ContainSingleType("Outer.Inner").Subject;

        var inner = type.Should().BeAssignableTo<MetadataNamedType>().Subject;
        inner.Name.Should().Be("Inner");
        inner.GenericArity.Should().Be(0);
        inner.GenericParameters.Should().BeEmpty();

        var outer = inner.ContainingType.Should().BeAssignableTo<MetadataNamedType>().Subject;
        outer.Name.Should().Be("Outer");
        outer.GenericArity.Should().Be(1);
        outer.GenericParameters.Should().ContainSingle().Subject.Name.Should().Be("T");
        outer.ContainingType.Should().BeNull();
    }

    [Fact]
    public void NamedType_Generic_Nesting_G_G()
    {
        var source = @"
            class Outer<K> {
                class Inner<V> {
                }
            }
        ";

        var assembly = MetadataFactory.CreateAssembly(source);
        var type = assembly.Should().ContainSingleType("Outer.Inner").Subject;

        var inner = type.Should().BeAssignableTo<MetadataNamedType>().Subject;
        inner.Name.Should().Be("Inner");
        inner.GenericArity.Should().Be(1);
        inner.GenericParameters.Should().ContainSingle().Subject.Name.Should().Be("V");

        var outer = inner.ContainingType.Should().BeAssignableTo<MetadataNamedType>().Subject;
        outer.Name.Should().Be("Outer");
        outer.GenericArity.Should().Be(1);
        outer.GenericParameters.Should().ContainSingle().Subject.Name.Should().Be("K");
        outer.ContainingType.Should().BeNull();
    }

    [Fact]
    public void NamedType_Generic_Nesting_N_G_N()
    {
        var source = @"
            class Outer {
                class Middle<V> {
                    class Inner {
                    }
                }
            }
        ";

        var assembly = MetadataFactory.CreateAssembly(source);
        var type = assembly.Should().ContainSingleType("Outer.Middle.Inner").Subject;

        var inner = type.Should().BeAssignableTo<MetadataNamedType>().Subject;
        inner.Name.Should().Be("Inner");
        inner.GenericArity.Should().Be(0);
        inner.GenericParameters.Should().BeEmpty();

        var middle = inner.ContainingType.Should().BeAssignableTo<MetadataNamedType>().Subject;
        middle.Name.Should().Be("Middle");
        middle.GenericArity.Should().Be(1);
        middle.GenericParameters.Should().ContainSingle().Subject.Name.Should().Be("V");

        var outer = middle.ContainingType.Should().BeAssignableTo<MetadataNamedType>().Subject;
        outer.Name.Should().Be("Outer");
        outer.GenericArity.Should().Be(0);
        outer.GenericParameters.Should().BeEmpty();
        outer.ContainingType.Should().BeNull();
    }

    [Fact]
    public void NamedType_Generic_Nesting_G_N_G()
    {
        var source = @"
            class Outer<K> {
                class Middle {
                    class Inner<V> {
                    }
                }
            }
        ";

        var assembly = MetadataFactory.CreateAssembly(source);
        var type = assembly.Should().ContainSingleType("Outer.Middle.Inner").Subject;

        var inner = type.Should().BeAssignableTo<MetadataNamedType>().Subject;
        inner.Name.Should().Be("Inner");
        inner.GenericArity.Should().Be(1);
        inner.GenericParameters.Should().ContainSingle().Subject.Name.Should().Be("V");

        var middle = inner.ContainingType.Should().BeAssignableTo<MetadataNamedType>().Subject;
        middle.Name.Should().Be("Middle");
        middle.GenericArity.Should().Be(0);
        middle.GenericParameters.Should().BeEmpty();

        var outer = middle.ContainingType.Should().BeAssignableTo<MetadataNamedType>().Subject;
        outer.Name.Should().Be("Outer");
        outer.GenericArity.Should().Be(1);
        outer.GenericParameters.Should().ContainSingle().Subject.Name.Should().Be("K");
        outer.ContainingType.Should().BeNull();
    }

    [Fact]
    public void NamedType_Generic_Parameters()
    {
        var source = @"
            class C1<T> { }
            class C2<K, V> { }
        ";

        var assembly = MetadataFactory.CreateAssembly(source);

        var c1 = assembly.Should().ContainSingleType("C1").Subject;
        c1.GenericParameters.Should().SatisfyRespectively(
            t =>
            {
                t.Name.Should().Be("T");
                t.Index.Should().Be(0);
                t.ContainingType.Should().BeSameAs(c1);
                t.ContainingMethod.Should().BeNull();
            }
        );

        var c2 = assembly.Should().ContainSingleType("C2").Subject;
        c2.GenericParameters.Should().SatisfyRespectively(
            k =>
            {
                k.Name.Should().Be("K");
                k.Index.Should().Be(0);
                k.ContainingType.Should().BeSameAs(c2);
                k.ContainingMethod.Should().BeNull();
            },
            v =>
            {
                v.Name.Should().Be("V");
                v.Index.Should().Be(1);
                v.ContainingType.Should().BeSameAs(c2);
                v.ContainingMethod.Should().BeNull();
            }
        );
    }

    [Fact]
    public void NamedType_Generic_Parameters_Variance()
    {
        var source = @"
            interface I1<T> { }
            interface I2<in T> { }
            interface I3<out T> { }
        ";

        var a = MetadataFactory.CreateAssembly(source);

        var i1 = a.Should().ContainSingleType("I1").Subject;
        i1.GenericParameters.Should().SatisfyRespectively(
            t => t.Variance.Should().Be(VarianceKind.None));

        var i2 = a.Should().ContainSingleType("I2").Subject;
        i2.GenericParameters.Should().SatisfyRespectively(
            t => t.Variance.Should().Be(VarianceKind.In));

        var i3 = a.Should().ContainSingleType("I3").Subject;
        i3.GenericParameters.Should().SatisfyRespectively(
            t => t.Variance.Should().Be(VarianceKind.Out));
    }

    [Fact]
    public void NamedType_Generic_Parameters_Constraint_New()
    {
        var source = @"
            class C1<T> where T: new() { }
            class C2<T> { }
        ";

        var a = MetadataFactory.CreateAssembly(source);

        var c1 = a.Should().ContainSingleType("C1").Subject;
        c1.GenericParameters.Should().SatisfyRespectively(
            t =>
            {
                t.HasConstraints().Should().BeTrue();
                t.HasConstructorConstraint.Should().BeTrue();
            });

        var c2 = a.Should().ContainSingleType("C2").Subject;
        c2.GenericParameters.Should().SatisfyRespectively(
            t =>
            {
                t.HasConstraints().Should().BeFalse();
                t.HasConstructorConstraint.Should().BeFalse();
            });
    }

    [Fact]
    public void NamedType_Generic_Parameters_Constraint_Class()
    {
        var source = @"
            class C1<T> where T: class { }
            class C2<T> { }
        ";

        var a = MetadataFactory.CreateAssembly(source);

        var c1 = a.Should().ContainSingleType("C1").Subject;
        c1.GenericParameters.Should().SatisfyRespectively(
            t =>
            {
                t.HasConstraints().Should().BeTrue();
                t.HasReferenceTypeConstraint.Should().BeTrue();
            });

        var c2 = a.Should().ContainSingleType("C2").Subject;
        c2.GenericParameters.Should().SatisfyRespectively(
            t =>
            {
                t.HasConstraints().Should().BeFalse();
                t.HasReferenceTypeConstraint.Should().BeFalse();
            });
    }
    
    [Fact]
    public void NamedType_Generic_Parameters_Constraint_Struct()
    {
        var source = @"
            class C1<T> where T: struct { }
            class C2<T> { }
        ";

        var a = MetadataFactory.CreateAssembly(source);

        var c1 = a.Should().ContainSingleType("C1").Subject;
        c1.GenericParameters.Should().SatisfyRespectively(
            t =>
            {
                t.HasConstraints().Should().BeTrue();
                t.HasValueTypeConstraint.Should().BeTrue();
            });

        var c2 = a.Should().ContainSingleType("C2").Subject;
        c2.GenericParameters.Should().SatisfyRespectively(
            t =>
            {
                t.HasConstraints().Should().BeFalse();
                t.HasValueTypeConstraint.Should().BeFalse();
            });
    }
    
    [Fact]
    public void NamedType_Generic_Parameters_Constraint_Interface()
    {
        var source = @"
            class C1<T> where T: I1 { }
            class C2<T> { }
            interface I1 {}
        ";

        var a = MetadataFactory.CreateAssembly(source);

        var c1 = a.Should().ContainSingleType("C1").Subject;
        c1.GenericParameters.Should().SatisfyRespectively(
            t =>
            {
                t.HasConstraints().Should().BeTrue();
                t.Constraints.Should().SatisfyRespectively(c =>
                {
                    c.ContraintType.Should().BeAssignableTo<MetadataNamedTypeReference>()
                        .Which.GetFullName().Should().Be("I1");
                });
            });

        var c2 = a.Should().ContainSingleType("C2").Subject;
        c2.GenericParameters.Should().SatisfyRespectively(
            t =>
            {
                t.HasConstraints().Should().BeFalse();
                t.Constraints.Should().BeEmpty();
            });
    }
}
