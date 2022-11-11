//#define COMPARE_DOC_IDS_WITH_CCI
//#define COMPARE_DOC_IDS_WITH_ROSLYN

#if COMPARE_DOC_IDS_WITH_CCI
using Microsoft.Cci;
using Microsoft.Cci.Extensions;
#endif

#if COMPARE_DOC_IDS_WITH_ROSLYN
using Microsoft.CodeAnalysis;
#endif

namespace Arroyo.Tests;

public sealed class DocumentationIdTests
{
    [Fact]
    public void DocumentationId_Namespaces()
    {
        var source = @"
            namespace N1
            {
                public static class Foo { }
            }

            namespace N1.N2
            {
                public static class Bar { }
            }
        ";

        var documentationIds = @"
            N:N1
            T:N1.Foo
            N:N1.N2
            T:N1.N2.Bar
        ";

        AssertMatch(source, documentationIds);
    }

    [Fact]
    public void DocumentationId_Classes()
    {
        var source = @"
            public static class C1 { }
            public class C2 { }
            public static class C3<T> { }
            public class C4<TKey, TValue> { }
        ";

        var documentationIds = @"
            T:C1
            T:C2
            M:C2.#ctor

            T:C3`1
            T:C4`2
            M:C4`2.#ctor
        ";

        AssertMatch(source, documentationIds);
    }

    [Fact]
    public void DocumentationId_Interfaces()
    {
        var source = @"
            public interface I1 { }
            public interface I2
            {
                void M();
            }
            public interface I3
            {
                virtual void M() { }
            }
            public interface I4
            {
                static void M() { }
            }
        ";

        var documentationIds = @"
            T:I1
            T:I2
            M:I2.M
            T:I3
            M:I3.M
            T:I4
            M:I4.M
        ";

        AssertMatch(source, documentationIds);
    }

    [Fact]
    public void DocumentationId_Structs()
    {
        var source = @"
            public struct S1 { }
            public struct S2
            {
                public S2() { }
            }
        ";

        var documentationIds = @"
            T:S1
            T:S2
            M:S2.#ctor
        ";

        AssertMatch(source, documentationIds);
    }

    [Fact]
    public void DocumentationId_Delegates()
    {
        var source = @"
            public delegate void D1();
            public delegate void D2(int x);
            public delegate void D3<T>(T x);
            public delegate TOut D3<TIn, TOut>(TIn x);
        ";

        var documentationIds = @"
            T:D1
            M:D1.#ctor(System.Object,System.IntPtr)
            M:D1.Invoke
            M:D1.BeginInvoke(System.AsyncCallback,System.Object)
            M:D1.EndInvoke(System.IAsyncResult)
            T:D2
            M:D2.#ctor(System.Object,System.IntPtr)
            M:D2.Invoke(System.Int32)
            M:D2.BeginInvoke(System.Int32,System.AsyncCallback,System.Object)
            M:D2.EndInvoke(System.IAsyncResult)
            T:D3`1
            M:D3`1.#ctor(System.Object,System.IntPtr)
            M:D3`1.Invoke(`0)
            M:D3`1.BeginInvoke(`0,System.AsyncCallback,System.Object)
            M:D3`1.EndInvoke(System.IAsyncResult)
            T:D3`2
            M:D3`2.#ctor(System.Object,System.IntPtr)
            M:D3`2.Invoke(`0)
            M:D3`2.BeginInvoke(`0,System.AsyncCallback,System.Object)
            M:D3`2.EndInvoke(System.IAsyncResult)
        ";

        AssertMatch(source, documentationIds);
    }

    [Fact]
    public void DocumentationId_Enums()
    {
        var source = @"
            public enum E1 { }
            public enum E2 { X, Y }
        ";

        var documentationIds = @"
            T:E1
            T:E2
            F:E2.X
            F:E2.Y
        ";

        AssertMatch(source, documentationIds);
    }

    [Fact]
    public void DocumentationId_Fields()
    {
        var source = @"
            public static class C1 {
                public static int F1;
            }
            public class C2 {
                public int F1;
                public volatile int F2;
            }
        ";

        var documentationIds = @"
            T:C1
            F:C1.F1
            T:C2
            F:C2.F1
            F:C2.F2
            M:C2.#ctor
        ";

        AssertMatch(source, documentationIds);
    }

    [Fact]
    public void DocumentationId_Methods()
    {
        var source = @"
            public static class C {
                public static void M1() {}
                public static void M2(int x) {}
                public static void M3(int x, string y) {}
                public static void M4(in int x) {}
                public static void M5(ref int x) {}
                public static void M6(out int x) { throw null; }
                public static void M7(params object[] x) { }
                public static void M8(int x, params object[] y) { }
                public static void M9<T>(T x) { }
                public static void M9<T1, T2>(T2 x, T1 y) { }
            }
            public static class C<TKey> {
                public static void M1(TKey x) {}
                public static void M2<T>(T x, TKey y) { }
                public static void M3<T1, T2>(T2 x, T1 y, TKey z) { }
            }
        ";

        var documentationIds = @"
            T:C
            M:C.M1
            M:C.M2(System.Int32)
            M:C.M3(System.Int32,System.String)
            M:C.M4(System.Int32@)
            M:C.M5(System.Int32@)
            M:C.M6(System.Int32@)
            M:C.M7(System.Object[])
            M:C.M8(System.Int32,System.Object[])
            M:C.M9``1(``0)
            M:C.M9``2(``1,``0)
            T:C`1
            M:C`1.M1(`0)
            M:C`1.M2``1(``0,`0)
            M:C`1.M3``2(``1,``0,`0)
        ";

        AssertMatch(source, documentationIds);
    }

    [Fact]
    public void DocumentationId_Properties()
    {
        var source = @"
            public static class C1 {
                public static int P1 { get; set; }
                public static int P2 { get; }
                public static int P3 { set {} }
            }
            public class C2 {
                public C2() { }
                public int this[float x] { get { throw null; } set { throw null; } }
                public int this[float x, double y] { get { throw null; } set { throw null; } }
            }
            public class C3 {
                public C3() { }
                [System.Runtime.CompilerServices.IndexerName(""Foos"")]
                public string this[int index] { get { throw null; } }
            }
        ";

        var documentationIds = @"
            T:C1
            M:C1.get_P1
            M:C1.set_P1(System.Int32)
            M:C1.get_P2
            M:C1.set_P3(System.Int32)
            P:C1.P1
            P:C1.P2
            P:C1.P3
            T:C2
            M:C2.#ctor
            M:C2.get_Item(System.Single)
            M:C2.set_Item(System.Single,System.Int32)
            M:C2.get_Item(System.Single,System.Double)
            M:C2.set_Item(System.Single,System.Double,System.Int32)
            P:C2.Item(System.Single)
            P:C2.Item(System.Single,System.Double)
            T:C3
            M:C3.#ctor
            M:C3.get_Foos(System.Int32)
            P:C3.Foos(System.Int32)           
        ";

        AssertMatch(source, documentationIds);
    }

    [Fact]
    public void DocumentationId_Events()
    {
        var source = @"
            using System;
            public static class C1 {
                public static event EventHandler Changed;
            }
        ";

        var documentationIds = @"
            T:C1
            M:C1.add_Changed(System.EventHandler)
            M:C1.remove_Changed(System.EventHandler)
            E:C1.Changed
        ";

        AssertMatch(source, documentationIds);
    }

    [Fact]
    public void DocumentationId_Operators()
    {
        var source = @"
            public class C {
                public C() { }
                public static C operator+(C operand) { throw null; }
                public static C operator-(C x, C y) { throw null; }
                public static explicit operator int (C operand) { throw null; }
                public static implicit operator float (C operand) { throw null; }
            }
        ";

        var documentationIds = @"
            T:C
            M:C.#ctor
            M:C.op_UnaryPlus(C)
            M:C.op_Subtraction(C,C)
            M:C.op_Explicit(C)~System.Int32
            M:C.op_Implicit(C)~System.Single
        ";

        AssertMatch(source, documentationIds);
    }

    [Fact]
    public void DocumentationId_NestedTypes()
    {
        var source = @"
            public static class C1 {
                public static void M1() { }
                public static class C2 {
                    public static void M2() { }
                }
            }
            public static class C2<TKey> {
                public static class C3<TValue> {
                    public static void M(TKey key, TValue value) { }
                }
                public static class C4 {
                    public static void M(TKey key) { }
       
                }
            }
        ";

        var documentationIds = @"
            T:C1
            M:C1.M1
            T:C1.C2
            M:C1.C2.M2
            T:C2`1
            T:C2`1.C3`1
            M:C2`1.C3`1.M(`0,`1)
            T:C2`1.C4
            M:C2`1.C4.M(`0)
        ";

        AssertMatch(source, documentationIds);
    }

    [Fact]
    public void DocumentationId_Signatures()
    {
        var source = @"
            using System.Collections.Generic;
            public static class C {
                public static void M1(IEnumerable<string> x) { }
                public static void M2(IEnumerable<string[]> x) { }
                public static void M3(KeyValuePair<string,int> x) { }
                public static unsafe void M4(int* x, float** y) { }
                public static unsafe void M5(int[][] arg) { }
                public static unsafe void M6(int[,] arg) { }
            }
        ";

        var documentationIds = @"
            T:C
            M:C.M1(System.Collections.Generic.IEnumerable{System.String})
            M:C.M2(System.Collections.Generic.IEnumerable{System.String[]})
            M:C.M3(System.Collections.Generic.KeyValuePair{System.String,System.Int32})
            M:C.M4(System.Int32*,System.Single**)
            M:C.M5(System.Int32[][])
            M:C.M6(System.Int32[0:,0:])
        ";

        AssertMatch(source, documentationIds);
    }

    [Fact]
    public void DocumentationId_Generics()
    {
        var source = @"
            public class OG<T1>
            {
                public class IN {}
                public class IG<T2> {}
            }
            public class ON
            {
                public class IN {}
                public class IG<T1> {}
            }
            public static class C {
                public static void M1(OG<int>.IN p) { }
                public static void M2(OG<int>.IG<string> p) { }
                public static void M3(ON.IN p) { }
                public static void M4(ON.IG<int> p) { }
            }
        ";

        var documentationIds = @"
            T:OG`1
            M:OG`1.#ctor
            T:OG`1.IN
            M:OG`1.IN.#ctor
            T:OG`1.IG`1
            M:OG`1.IG`1.#ctor
            T:ON
            M:ON.#ctor
            T:ON.IN
            M:ON.IN.#ctor
            T:ON.IG`1
            T:C
            M:ON.IG`1.#ctor
            M:C.M1(OG{System.Int32}.IN)
            M:C.M2(OG{System.Int32}.IG{System.String})
            M:C.M3(ON.IN)
            M:C.M4(ON.IG{System.Int32})
        ";

        AssertMatch(source, documentationIds);
    }

    [Fact]
    public void DocumentationId_FunctionPointers()
    {
        var source = @"
            public static class C {
                public static unsafe void M1(delegate*<void> f) { }
                public static unsafe void M2(delegate*<int, void> f) { }
                public static unsafe void M3(delegate*<double*, float, float*> f) { }
                public static unsafe void M4(delegate* unmanaged[Cdecl]<int, void> f) { }
                public static unsafe void M5(delegate* unmanaged[Stdcall]<void> f) { }
            }
        ";

        var documentationIds = @"
            T:C
            M:C.M1(function System.Void ())
            M:C.M2(function System.Void (System.Int32))
            M:C.M3(function System.Single* (System.Double*,System.Single))
            M:C.M4(function System.Void (System.Int32))
            M:C.M5(function System.Void ())
        ";

        AssertMatch(source, documentationIds);
    }

    [Fact]
    public void DocumentationId_VarArgs()
    {
        var source = @"
            public static class Bar {
                public static void M1(__arglist) { }
                public static void M2(int x, __arglist) { }
            }
        ";

        var documentationIds = @"
            T:Bar
            M:Bar.M1()
            M:Bar.M2(System.Int32,)
        ";

        AssertMatch(source, documentationIds);
    }

    private static void AssertMatch(string source,
                                    string expectedDocumentationIdsText)
    {
        var expectedDocumentationIds = expectedDocumentationIdsText.Split('\n', StringSplitOptions.TrimEntries |
                                                                                StringSplitOptions.RemoveEmptyEntries);

        var assembly = MetadataFactory.CreateAssembly(source);
        var acutalDocumentationIds = new List<string>();
        WalkAssembly(assembly, acutalDocumentationIds);

#if COMPARE_DOC_IDS_WITH_CCI
        CompareWithCciDocumentationIds(source, expectedDocumentationIds);
#endif

#if COMPARE_DOC_IDS_WITH_ROSLYN
        CompareWithRoslynDocumentationIds(source, expectedDocumentationIds);
#endif
        
#if COMPARE_DOC_IDS_WITH_CCI ||COMPARE_DOC_IDS_WITH_ROSLYN
        return;
#endif

        var expectedList = string.Join(", ", expectedDocumentationIds.OrderBy(x => x));
        var actualList = string.Join(", ", acutalDocumentationIds.OrderBy(x => x));

        actualList.Should().Be(expectedList);

        static void WalkAssembly(MetadataAssembly assembly, List<string> documentationIds)
        {
            WalkNamespace(assembly.MainModule.NamespaceRoot, documentationIds);
        }

        static void WalkNamespace(MetadataNamespace ns, List<string> documentationIds)
        {
            foreach (var member in ns.Members)
            {
                if (member is MetadataNamespace nestedNamespace)
                {
                    if (!IsNamespaceMemberVisibleOutside(nestedNamespace))
                        continue;

                    var id = nestedNamespace.GetDocumentationId();
                    if (id is not null)
                        documentationIds.Add(id);
                    
                    WalkNamespace(nestedNamespace, documentationIds);
                }
                else
                {
                    var type = (MetadataNamedType)member;
                    if (!IsTypeMemberVisibleOutside(type))
                        continue;

                    var id = type.GetDocumentationId();
                    if (id is not null)
                        documentationIds.Add(id);
                    
                    WalkType(type, documentationIds);
                }
            }
        }

        static void WalkType(MetadataNamedType type, List<string> documentationIds)
        {
            foreach (var member in type.Members)
            {
                // Don't include value__ magic field
                if (member is MetadataField f && !f.IsStatic && f.ContainingType.Kind == TypeKind.Enum)
                    continue;

                if (!IsTypeMemberVisibleOutside(member))
                    continue;

                var id = member.GetDocumentationId();
                if (id is not null)
                    documentationIds.Add(id);

                if (member is MetadataNamedType nestedType)
                    WalkType(nestedType, documentationIds);
            }
        }

        static bool IsNamespaceMemberVisibleOutside(IMetadataNamespaceMember member)
        {
            switch (member)
            {
                case MetadataNamedType type:
                    return IsVisibleOutside(type.Accessibility);
                case MetadataNamespace @namespace:
                    return @namespace.Members.Any(IsNamespaceMemberVisibleOutside);
                default:
                    throw new Exception($"Unexpected namespace member {member}");
            }
        }
        
        static bool IsTypeMemberVisibleOutside(IMetadataTypeMember typeOrMember)
        {
            switch (typeOrMember)
            {
                case MetadataNamedType type:
                    return IsVisibleOutside(type.Accessibility);
                case MetadataEvent metadataEvent:
                    return IsVisibleOutside(metadataEvent.Accessibility);
                case MetadataField metadataField:
                    return IsVisibleOutside(metadataField.Accessibility);
                case MetadataMethod metadataMethod:
                    return IsVisibleOutside(metadataMethod.Accessibility);
                case MetadataProperty metadataProperty:
                    return IsVisibleOutside(metadataProperty.Accessibility);
                default:
                    throw new Exception($"Unexpected type member {typeOrMember}");
            }
        }

        static bool IsVisibleOutside(MetadataAccessibility accessibility)
        {
            switch (accessibility)
            {
                case MetadataAccessibility.Public:
                case MetadataAccessibility.Family:
                case MetadataAccessibility.FamilyOrAssembly:
                    return true;

                case MetadataAccessibility.Private:
                case MetadataAccessibility.FamilyAndAssembly:
                case MetadataAccessibility.Assembly:
                    return false;

                default:
                    throw new Exception($"Unexpected accessibility: {accessibility}");
            }
        }
    }

#if COMPARE_DOC_IDS_WITH_CCI

    private static void CompareWithCciDocumentationIds(string source, string[] expectedDocumentationIds)
    {
        var cciAssembly = MetadataFactory.CreateCciAssembly(source);
        var cciDocumentationIds = new List<string>();
        WalkAssembly(cciAssembly, cciDocumentationIds);

        var roslynExpectedList = string.Join(", ", cciDocumentationIds.OrderBy(x => x));
        var roslynActualList = string.Join(", ", expectedDocumentationIds.OrderBy(x => x));

        Assertion.Equal(roslynActualList, roslynExpectedList);

        static void WalkAssembly(IAssembly assembly, List<string> documentationIds)
        {
            WalkNamespace(assembly.NamespaceRoot, documentationIds);
        }

        static void WalkNamespace(INamespaceDefinition ns, List<string> documentationIds)
        {
            foreach (var type in ns.Members.OfType<INamedTypeDefinition>())
            {
                if (!IsMemberVisibleOutside(type))
                    continue;

                documentationIds.Add(type.DocId());
                WalkType(type, documentationIds);
            }

            foreach (var nestedNamespace in ns.Members.OfType<INamespaceDefinition>())
            {
                documentationIds.Add(nestedNamespace.DocId());
                WalkNamespace(nestedNamespace, documentationIds);
            }
        }

        static void WalkType(INamedTypeDefinition type, List<string> documentationIds)
        {
            foreach (var member in type.Members)
            {
                if (!IsMemberVisibleOutside(member))
                    continue;

                // Don't include value__ magic field
                if (member is IFieldDefinition f && !f.IsStatic && f.ContainingType.IsEnum)
                    continue;

                var id = member.DocId();
                if (id is not null)
                    documentationIds.Add(id);

                if (member is INamedTypeDefinition nestedType)
                    WalkType(nestedType, documentationIds);
            }
        }

        static bool IsMemberVisibleOutside(IDefinition definition)
        {
            if (definition is ITypeDefinition type)
                return IsVisibleOutside(type.GetVisibility());

            if (definition is ITypeDefinitionMember member)
                return IsVisibleOutside(member.Visibility);

            return false;
        }

        static bool IsVisibleOutside(TypeMemberVisibility visibility)
        {
            switch (visibility)
            {
                case TypeMemberVisibility.Public:
                case TypeMemberVisibility.Family:
                case TypeMemberVisibility.FamilyOrAssembly:
                    return true;
                case TypeMemberVisibility.Assembly:
                case TypeMemberVisibility.FamilyAndAssembly:
                case TypeMemberVisibility.Private:
                    return false;
                // case TypeMemberVisibility.Other:
                // case TypeMemberVisibility.Mask:
                default:
                    throw new Exception($"Unexpected accessibility: {visibility}");
            }
        }
    }

#endif

#if COMPARE_DOC_IDS_WITH_ROSLYN

    private static void CompareWithRoslynDocumentationIds(string source, string[] expectedDocumentationIds)
    {
        var roslynAssembly = MetadataFactory.CreateRoslynAssembly(source);
        var roslynDocumentationIds = new List<string>();
        WalkAssembly(roslynAssembly, roslynDocumentationIds);

        var roslynExpectedList = string.Join(", ", roslynDocumentationIds.OrderBy(x => x));
        var roslynActualList = string.Join(", ", expectedDocumentationIds.OrderBy(x => x));

        Assertion.Equal(roslynActualList, roslynExpectedList);

        static void WalkAssembly(IAssemblySymbol assembly, List<string> documentationIds)
        {
            WalkNamespace(assembly.GlobalNamespace, documentationIds);
        }

        static void WalkNamespace(INamespaceSymbol ns, List<string> documentationIds)
        {
            foreach (var type in ns.GetTypeMembers())
            {
                if (!IsMemberVisibleOutside(type))
                    continue;

                documentationIds.Add(type.GetDocumentationCommentId()!);
                WalkType(type, documentationIds);
            }

            foreach (var nestedNamespace in ns.GetNamespaceMembers())
            {
                documentationIds.Add(nestedNamespace.GetDocumentationCommentId()!);
                WalkNamespace(nestedNamespace, documentationIds);
            }
        }

        static void WalkType(INamedTypeSymbol type, List<string> documentationIds)
        {
            foreach (var member in type.GetMembers())
            {
                if (member is IMethodSymbol m &&
                    m.MethodKind == Microsoft.CodeAnalysis.MethodKind.Constructor)
                {
                    // Don't include enum constructors
                    if (m.ContainingType.TypeKind == Microsoft.CodeAnalysis.TypeKind.Enum)
                        continue;

                    // Only include struct constructors if they were declared
                    if (m.ContainingType.IsValueType && m.IsImplicitlyDeclared)
                        continue;
                }

                if (!IsMemberVisibleOutside(member))
                    continue;

                var id = member.GetDocumentationCommentId();
                if (id is not null)
                    documentationIds.Add(id);

                if (member is INamedTypeSymbol nestedType)
                    WalkType(nestedType, documentationIds);
            }
        }

        static bool IsMemberVisibleOutside(ISymbol symbol)
        {
            return IsVisibleOutside(symbol.DeclaredAccessibility);
        }

        static bool IsVisibleOutside(Accessibility accessibility)
        {
            switch (accessibility)
            {
                case Accessibility.Public:
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    return true;

                case Accessibility.Private:
                case Accessibility.ProtectedAndInternal:
                case Accessibility.Internal:
                    return false;

                default:
                    throw new Exception($"Unexpected accessibility: {accessibility}");
            }
        }
    }

#endif
}
