using System.Reflection.Metadata;

namespace Arroyo.Tests;

public sealed class MethodTests
{
    [Fact]
    public void Method_ContainingType()
    {
        var source = @"
            class C {
                void M() {}
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var c = a.Should().ContainSingleType("C").Subject;
        var m = a.Should().ContainSingleMethod("C.M").Subject;

        m.ContainingType.Should().BeSameAs(c);
    }

    // TODO: IsPrivateScope

    [Fact]
    public void Method_IsStatic()
    {
        var source = @"
            class C {
                static void M1() {}
                       void M2() {}
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var m1 = a.Should().ContainSingleMethod("C.M1").Subject;
        var m2 = a.Should().ContainSingleMethod("C.M2").Subject;

        m1.IsStatic.Should().BeTrue();
        m2.IsStatic.Should().BeFalse();
    }
    
    [Fact]
    public void Method_IsSealed()
    {
        var source = @"
            class B {
                protected virtual void M() {}
            }
            class C1 : B {
                protected sealed override void M() {}
            }
            class C2 : B {
                protected override void M() {}
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var c1m = a.Should().ContainSingleMethod("C1.M").Subject;
        var c2m = a.Should().ContainSingleMethod("C2.M").Subject;

        c1m.IsSealed.Should().BeTrue();
        c2m.IsSealed.Should().BeFalse();
    }
    
    [Fact]
    public void Method_IsVirtual()
    {
        var source = @"
            class C {
                public virtual void M1() {}
                public         void M2() {}
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var m1 = a.Should().ContainSingleMethod("C.M1").Subject;
        var m2 = a.Should().ContainSingleMethod("C.M2").Subject;

        m1.IsVirtual.Should().BeTrue();
        m2.IsVirtual.Should().BeFalse();
    }
    
    // TODO: IsHideBySig
    // TODO: CheckAccessOnOverride

    [Fact]
    public void Method_IsAbstract()
    {
        var source = @"
            abstract class C {
                public abstract void M1();
                public          void M2() {}
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var m1 = a.Should().ContainSingleMethod("C.M1").Subject;
        var m2 = a.Should().ContainSingleMethod("C.M2").Subject;

        m1.IsAbstract.Should().BeTrue();
        m2.IsAbstract.Should().BeFalse();
    }
    
    // TODO: IsSpecialName
    // TODO: IsPinvokeImplementation
    // TODO: IsUnmanagedExport
    // TODO: IsRuntimeSpecialName
    // TODO: HasSecurity
    // TODO: RequireSecurityObject
    
    [Fact]
    public void Method_IsOverride()
    {
        var source = @"
            class B {
                protected virtual void M() {}
            }
            class C1 : B {
                protected virtual void M() {}
            }
            class C2 : B {
                protected override void M() {}
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var c1m = a.Should().ContainSingleMethod("C1.M").Subject;
        var c2m = a.Should().ContainSingleMethod("C2.M").Subject;

        c1m.IsOverride.Should().BeFalse();
        c2m.IsOverride.Should().BeTrue();
    }

    [Fact]
    public void Method_ReturnType()
    {
        var source = @"
            class C {
                void M() {}
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var m = a.Should().ContainSingleMethod("C.M").Subject;

        var t = m.ReturnType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        t.GetFullName().Should().Be("System.Void");
    }

    [Fact]
    public void Method_ReturnType_ref()
    {
        var source = @"
            class C {
                ref int M() => throw null;
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var m = a.Should().ContainSingleMethod("C.M").Subject;

        m.GetCustomAttributes().Should().BeEmpty();
        m.CustomModifiers.Should().BeEmpty();
        m.RefCustomModifiers.Should().BeEmpty();
        m.RefKind.Should().Be(MetadataRefKind.Ref);
        var mt = m.ReturnType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        mt.GetFullName().Should().Be("System.Int32");
    }
    
    [Fact]
    public void Method_ReturnType_ref_readonly()
    {
        var source = @"
            class C {
                ref readonly int M() => throw null;
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var m = a.Should().ContainSingleMethod("C.M").Subject;

        m.GetCustomAttributes().Should().BeEmpty();
        m.CustomModifiers.Should().BeEmpty();
        
        var mm = m.RefCustomModifiers.Should().ContainSingle().Subject;
        mm.IsRequired.Should().BeTrue();
        var mmt = mm.Type.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        mmt.GetFullName().Should().Be("System.Runtime.InteropServices.InAttribute");
        
        m.RefKind.Should().Be(MetadataRefKind.In);
        var mt = m.ReturnType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        mt.GetFullName().Should().Be("System.Int32");
    }

    [Theory]
    [TestData(nameof(TestData.Accessibility))]
    public void Method_Accessibility(string modifier, MetadataAccessibility expectedAccessibility)
    {
        var source = @$"
            class C {{
                {modifier} void M() {{}}
            }}
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var f = a.Should().ContainSingleMethod("C.M").Subject;
        
        f.Accessibility.Should().Be(expectedAccessibility);
    }
    
    [Theory]
    [TestData(nameof(TestData.OperatorMethods))]
    public void Method_Kind_Operators(string methodName, MethodKind expectedKind)
    {
        var source = @"
            class C {
                public static explicit operator int(C x) { throw null; }
                public static implicit operator C(int x) { throw null; }
                public static bool operator false(C x) { throw null; }
                public static bool operator true(C x) { throw null; }

                public static C operator -- (C x) { throw null; }
                public static C operator ++ (C x) { throw null; }

                public static C operator ~ (C x) { throw null; }
                public static C operator - (C x) { throw null; }
                public static C operator + (C x) { throw null; }

                public static C operator  + (C x, C y) { throw null; }
                public static C operator  / (C x, C y) { throw null; }
                public static C operator  - (C x, C y) { throw null; }
                public static C operator  * (C x, C y) { throw null; }
                public static C operator  % (C x, C y) { throw null; }

                public static C operator  & (C x, C y) { throw null; }
                public static C operator  | (C x, C y) { throw null; }

                public static C operator  == (C x, C y) { throw null; }
                public static C operator  != (C x, C y) { throw null; }
                public static C operator  < (C x, C y) { throw null; }
                public static C operator  <= (C x, C y) { throw null; }
                public static C operator  > (C x, C y) { throw null; }
                public static C operator  >= (C x, C y) { throw null; }

                public static C operator  ^ (C x, C y) { throw null; }
                public static C operator  << (C x, int y) { throw null; }
                public static C operator  >> (C x, int y) { throw null; }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var m = a.Should().ContainSingleMethod("C." + methodName).Subject; 

        m.Kind.Should().Be(expectedKind);
    }

    [Fact]
    public void Method_Kind_Ordinary()
    {
        var source = @"
            class C {
                void M() {}
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var m = a.Should().ContainSingleMethod("C.M").Subject;

        m.Kind.Should().Be(MethodKind.Ordinary);
    }

    [Fact]
    public void Method_Kind_Constructor()
    {
        var source = @"
            class C {
                C() {}
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var m = a.Should().ContainSingleMethod("C.#ctor").Subject;

        m.Kind.Should().Be(MethodKind.Constructor);
    }

    [Fact]
    public void Method_Kind_ClassConstructor()
    {
        var source = @"
            class C {
                static C() {}
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var m = a.Should().ContainSingleMethod("C.#cctor").Subject;

        m.Kind.Should().Be(MethodKind.ClassConstructor);
    }

    [Fact]
    public void Method_Kind_Finalizer()
    {
        var source = @"
            class C {
                ~C() {}
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var m = a.Should().ContainSingleMethod("C.Finalize").Subject;

        m.Kind.Should().Be(MethodKind.Finalizer);
    }

    [Fact]
    public void Method_GenericParameters_GenericArity()
    {
        var source = @"
            class C {
                void M1() {}
                void M2<T1>() {}
                void M3<T1,T2>() {}
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);

        var m1 = a.Should().ContainSingleMethod("C.M1").Subject;
        var m2 = a.Should().ContainSingleMethod("C.M2").Subject;
        var m3 = a.Should().ContainSingleMethod("C.M3").Subject;

        m1.GenericArity.Should().Be(0);
        m1.GenericParameters.Should().BeEmpty();
        
        m2.GenericArity.Should().Be(1);
        var m2t1 = m2.GenericParameters.Should().ContainSingle().Subject;
        m2t1.Name.Should().Be("T1");
        
        m3.GenericArity.Should().Be(2);
        m3.GenericParameters.Should().SatisfyRespectively(
            p =>
            {
                p.Name.Should().Be("T1");
            },
            p =>
            {
                p.Name.Should().Be("T2");
            }
        );
    }

    [Fact]
    public void Method_Parameters()
    {
        var source = @"
            class C {
                void M1() {}
                void M2(int x) {}
                void M3(int x, float y) {}
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);

        var m1 = a.Should().ContainSingleMethod("C.M1").Subject;
        var m2 = a.Should().ContainSingleMethod("C.M2").Subject;
        var m3 = a.Should().ContainSingleMethod("C.M3").Subject;

        m1.Parameters.Should().BeEmpty();
        
        var m2t1 = m2.Parameters.Should().ContainSingle().Subject;
        m2t1.Name.Should().Be("x");
        m2t1.ParameterType.GetDocumentationId().Should().Be("T:System.Int32");
        
        m3.Parameters.Should().SatisfyRespectively(
            p =>
            {
                p.Name.Should().Be("x");
                p.ParameterType.GetDocumentationId().Should().Be("T:System.Int32");
            },
            p =>
            {
                p.Name.Should().Be("y");
                p.ParameterType.GetDocumentationId().Should().Be("T:System.Single");
            }
        );
    }

    [Fact]
    public void Method_Parameters_In_Out()
    {
        var source = @"
            using System.Runtime.InteropServices;
            class C {
                void M0(int x) => throw null;
                void M1([In] int x) => throw null;
                void M2([In, Out] int x) => throw null;
                void M3([Out] int x) => throw null;
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);

        var m0 = a.Should().ContainSingleMethod("C.M0").Subject;
        var m1 = a.Should().ContainSingleMethod("C.M1").Subject;
        var m2 = a.Should().ContainSingleMethod("C.M2").Subject;
        var m3 = a.Should().ContainSingleMethod("C.M3").Subject;

        var m0p = m0.Parameters.Should().ContainSingle().Subject;
        m0p.IsIn.Should().BeFalse();
        m0p.IsOut.Should().BeFalse();

        var m1p = m1.Parameters.Should().ContainSingle().Subject;
        m1p.IsIn.Should().BeTrue();
        m1p.IsOut.Should().BeFalse();
        
        var m2p = m2.Parameters.Should().ContainSingle().Subject;
        m2p.IsIn.Should().BeTrue();
        m2p.IsOut.Should().BeTrue();

        var m3p = m3.Parameters.Should().ContainSingle().Subject;
        m3p.IsIn.Should().BeFalse();
        m3p.IsOut.Should().BeTrue();
    }

    [Fact]
    public void Method_Parameters_in_out_ref()
    {
        var source = @"
            class C {
                void M0(int x) => throw null;
                void M1(in int x) => throw null;
                void M2(out int x) => throw null;
                void M3(ref int x) => throw null;
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        
        var m0 = a.Should().ContainSingleMethod("C.M0").Subject;
        var m0p = m0.Parameters.Should().ContainSingle().Subject;
        m0p.IsIn.Should().BeFalse();
        m0p.IsOut.Should().BeFalse();
        m0p.RefKind.Should().Be(MetadataRefKind.None);
        var m0pt = m0p.ParameterType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        m0pt.GetFullName().Should().Be("System.Int32");

        // in
        var m1 = a.Should().ContainSingleMethod("C.M1").Subject;
        var m1p = m1.Parameters.Should().ContainSingle().Subject;
        m1p.IsIn.Should().BeTrue();
        m1p.IsOut.Should().BeFalse();
        m1p.RefKind.Should().Be(MetadataRefKind.In);
        var m1pt = m0p.ParameterType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        m1pt.GetFullName().Should().Be("System.Int32");
        
        // out
        var m2 = a.Should().ContainSingleMethod("C.M2").Subject;
        var m2p = m2.Parameters.Should().ContainSingle().Subject;
        m2p.IsIn.Should().BeFalse();
        m2p.IsOut.Should().BeTrue();
        m2p.RefKind.Should().Be(MetadataRefKind.Out);
        var m2pt = m0p.ParameterType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        m2pt.GetFullName().Should().Be("System.Int32");

        // ref
        var m3 = a.Should().ContainSingleMethod("C.M3").Subject;
        var m3p = m3.Parameters.Should().ContainSingle().Subject;
        m3p.IsIn.Should().BeFalse();
        m3p.IsOut.Should().BeFalse();
        m3p.RefKind.Should().Be(MetadataRefKind.Ref);
        var m3pt = m0p.ParameterType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        m3pt.GetFullName().Should().Be("System.Int32");
    }

    [Fact]
    public void Method_Parameters_HasDefault_DefaultValue_IsOptional()
    {
        var source = @"
            class C {
                void M1(int x) { }
                void M2(int x, int y = 42, string z = ""test"") { }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        
        var m1 = a.Should().ContainSingleMethod("C.M1").Subject;
        var m1p1 = m1.Parameters.Should().ContainSingle().Subject;
        m1p1.IsOptional.Should().BeFalse();
        m1p1.HasDefault.Should().BeFalse();
        m1p1.DefaultValue.Should().BeNull();
       
        var m2 = a.Should().ContainSingleMethod("C.M2").Subject;
        m2.Parameters.Should().SatisfyRespectively(
            p =>
            {
                p.IsOptional.Should().BeFalse();
                p.HasDefault.Should().BeFalse();
                p.DefaultValue.Should().BeNull();
            },
            p =>
            {
                p.IsOptional.Should().BeTrue();
                p.HasDefault.Should().BeTrue();
                p.DefaultValue.Should().NotBeNull();
                p.DefaultValue!.Value.Should().Be(42);
            },
            p =>
            {
                p.IsOptional.Should().BeTrue();
                p.HasDefault.Should().BeTrue();
                p.DefaultValue.Should().NotBeNull();
                p.DefaultValue!.Value.Should().Be("test");
            }
        );
    }

    [Fact]
    public void Method_Parameters_Params()
    {
        var source = @"
            class C {
                void M1(object[] args) { }
                void M2(params object[] args) { }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);

        var m1 = a.Should().ContainSingleMethod("C.M1").Subject;
        var m1p1 = m1.Parameters.Should().ContainSingle().Subject;
        m1p1.IsParams.Should().BeFalse();
        m1p1.GetCustomAttributes().Should().BeEmpty();
        m1p1.GetCustomAttributes(includeProcessed: true).Should().BeEmpty();

        var m2 = a.Should().ContainSingleMethod("C.M2").Subject;
        var m2p1 = m2.Parameters.Should().ContainSingle().Subject;
        m2p1.IsParams.Should().BeTrue();
        m2p1.GetCustomAttributes().Should().BeEmpty();
        m2p1.Should()
            .ContainSingleAttribute("System.ParamArrayAttribute");
    }
    
    [Fact]
    public void Method_Parameters_arglist()
    {
        var source = @"
            class C {
                void M1() { }
                void M2(__arglist) { }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        
        var m1 = a.Should().ContainSingleMethod("C.M1").Subject;
        m1.IsVararg.Should().BeFalse();
        m1.Signature.CallingConvention.Should().Be(SignatureCallingConvention.Default);
       
        var m2 = a.Should().ContainSingleMethod("C.M2").Subject;
        m2.IsVararg.Should().BeTrue();
        m2.Signature.CallingConvention.Should().Be(SignatureCallingConvention.VarArgs);
    }

    // TODO: RelativeVirtualAddress

    [Fact]
    public void Method_CustomAttribute()
    {
        var source = @"
            class C {
                [D]
                void M() {}
            }
            class D : System.Attribute {}            
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var m = a.Should().ContainSingleMethod("C.M").Subject;

        var ca = m.GetCustomAttributes().Single();
        ca.Constructor.ContainingType.GetDocumentationId().Should().Be("T:D");
    }
    
    [Fact]
    public void Method_Generic_Parameters()
    {
        var source = @"
            class C {
                void M1<T>() { }
                void M2<K, V>() { }
            }
        ";

        var assembly = MetadataFactory.CreateAssembly(source);

        var c = assembly.Should().ContainSingleType("C").Subject;

        var m1 = assembly.Should().ContainSingleMethod("C.M1").Subject;
        m1.GenericParameters.Should().SatisfyRespectively(
            t =>
            {
                t.Name.Should().Be("T");
                t.Index.Should().Be(0);
                t.ContainingType.Should().BeSameAs(c);
                t.ContainingMethod.Should().BeSameAs(m1);
            }
        );

        var m2 = assembly.Should().ContainSingleMethod("C.M2").Subject;
        m2.GenericParameters.Should().SatisfyRespectively(
            k =>
            {
                k.Name.Should().Be("K");
                k.Index.Should().Be(0);
                k.ContainingType.Should().BeSameAs(c);
                k.ContainingMethod.Should().BeSameAs(m2);
            },
            v =>
            {
                v.Name.Should().Be("V");
                v.Index.Should().Be(1);
                v.ContainingType.Should().BeSameAs(c);
                v.ContainingMethod.Should().BeSameAs(m2);
            }
        );
    }

    [Fact]
    public void Method_Generic_Parameters_Variance()
    {
        var source = @"
            class C {
                void M<T>() { }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var m = a.Should().ContainSingleMethod("C.M").Subject;
        m.GenericParameters.Should().SatisfyRespectively(
            t => t.Variance.Should().Be(VarianceKind.None));
    }

    [Fact]
    public void Method_Generic_Parameters_Constraint_New()
    {
        var source = @"
            class C {
                void M1<T>() where T: new() { }
                void M2<T>() { }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);

        var m1 = a.Should().ContainSingleMethod("C.M1").Subject;
        m1.GenericParameters.Should().SatisfyRespectively(
            t =>
            {
                t.HasConstraints().Should().BeTrue();
                t.HasConstructorConstraint.Should().BeTrue();
            });

        var m2 = a.Should().ContainSingleMethod("C.M2").Subject;
        m2.GenericParameters.Should().SatisfyRespectively(
            t =>
            {
                t.HasConstraints().Should().BeFalse();
                t.HasConstructorConstraint.Should().BeFalse();
            });
    }

    [Fact]
    public void Method_Generic_Parameters_Constraint_Class()
    {
        var source = @"
            class C {
                void M1<T>() where T: class { }
                void M2<T>() { }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);

        var m1 = a.Should().ContainSingleMethod("C.M1").Subject;
        m1.GenericParameters.Should().SatisfyRespectively(
            t =>
            {
                t.HasConstraints().Should().BeTrue();
                t.HasReferenceTypeConstraint.Should().BeTrue();
            });

        var m2 = a.Should().ContainSingleMethod("C.M2").Subject;
        m2.GenericParameters.Should().SatisfyRespectively(
            t =>
            {
                t.HasConstraints().Should().BeFalse();
                t.HasReferenceTypeConstraint.Should().BeFalse();
            });
    }
    
    [Fact]
    public void Method_Generic_Parameters_Constraint_Struct()
    {
        var source = @"
            class C {
                void M1<T>() where T: struct { }
                void M2<T>() { }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);

        var m1 = a.Should().ContainSingleMethod("C.M1").Subject;
        m1.GenericParameters.Should().SatisfyRespectively(
            t =>
            {
                t.HasConstraints().Should().BeTrue();
                t.HasValueTypeConstraint.Should().BeTrue();
            });

        var m2 = a.Should().ContainSingleMethod("C.M2").Subject;
        m2.GenericParameters.Should().SatisfyRespectively(
            t =>
            {
                t.HasConstraints().Should().BeFalse();
                t.HasValueTypeConstraint.Should().BeFalse();
            });
    }
    
    [Fact]
    public void Method_Generic_Parameters_Constraint_Interface()
    {
        var source = @"
            class C {
                void M1<T>() where T: I1 { }
                void M2<T>() { }
            }
            interface I1 {}
        ";

        var a = MetadataFactory.CreateAssembly(source);

        var m1 = a.Should().ContainSingleMethod("C.M1").Subject;
        m1.GenericParameters.Should().SatisfyRespectively(
            t =>
            {
                t.HasConstraints().Should().BeTrue();
                t.Constraints.Should().SatisfyRespectively(c =>
                {
                    c.ContraintType.Should().BeAssignableTo<MetadataNamedTypeReference>()
                     .Which.GetFullName().Should().Be("I1");
                });
            });

        var m2 = a.Should().ContainSingleMethod("C.M2").Subject;
        m2.GenericParameters.Should().SatisfyRespectively(
            t =>
            {
                t.HasConstraints().Should().BeFalse();
                t.Constraints.Should().BeEmpty();
            });
    }
    
    // NOTE: AssociatedMember is covered by Property and Event tests
    // NOTE: Operations is covered by IL tests
}
