using System.Reflection.Metadata;

namespace Arroyo.Tests;

public sealed class OperationTests
{
    [Fact]
    public void Operations_Call_Definition()
    {
        var source = @"
            class C {
                void M1() { M2(); }
                void M2() { }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var m1 = a.Should().ContainSingleMethod("C.M1").Subject;
        var m2 = a.Should().ContainSingleMethod("C.M2").Subject;

        var call = m1.GetOperations().Single(o => o.OpCode == ILOpCode.Call);
        call.Argument.Should().BeAssignableTo<MetadataMethod>().Subject.Should().BeSameAs(m2);
    }
    
    [Fact]
    public void Operations_Call_Reference()
    {
        var baseSource = @"
            public class CBase {
                public void MBase() { }
            }
        ";

        var derivedSource = @"
            class CDerived : CBase {
                void MDerived() {
                    MBase(); 
                }
            }
        ";

        var baseAC = MetadataFactory.CreateCompilation(baseSource);
        var baseA = baseAC.ToMetadataAssembly();

        var derivedAC = MetadataFactory.CreateCompilation(derivedSource, new[] { baseAC });
        var derivedA = derivedAC.ToMetadataAssembly();

        var mbase = baseA.Should().ContainSingleMethod("CBase.MBase").Subject;
        var mderived = derivedA.Should().ContainSingleMethod("CDerived.MDerived").Subject;

        var call = mderived.GetOperations().Single(o => o.OpCode == ILOpCode.Call);
        call.Argument.Should().BeAssignableTo<MetadataMethodReference>()
            .Which.GetDocumentationId().Should().Be(mbase.GetDocumentationId());
    }
}