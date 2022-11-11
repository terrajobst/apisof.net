namespace Arroyo.Tests;

public sealed class EventTests
{
    [Fact]
    public void Event_ContainingType()
    {
        var source = @"
            class C {
                event System.EventHandler E;
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var c = a.Should().ContainSingleType("C").Subject;
        var e = a.Should().ContainSingleEvent("C.E").Subject;

        e.ContainingType.Should().BeSameAs(c);
    }
    
    [Fact]
    public void Event_EventType()
    {
        var source = @"
            class C {
                event System.EventHandler E;
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var e = a.Should().ContainSingleEvent("C.E").Subject;

        var t = e.EventType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        t.GetFullName().Should().Be("System.EventHandler");
    }

    [Fact]
    public void Event_AssociatedMember_Adder_Remover_Accessors()
    {
        var source = @"
            class C {
                event System.EventHandler E;
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var e = a.Should().ContainSingleEvent("C.E").Subject;
        var adder = a.Should().ContainSingleMethod("C.add_E").Subject;
        var remover = a.Should().ContainSingleMethod("C.remove_E").Subject;

        e.Adder.Should().BeSameAs(adder);
        e.Remover.Should().BeSameAs(remover);
        e.Raiser.Should().BeNull();
        adder.AssociatedMember.Should().BeSameAs(e);
        remover.AssociatedMember.Should().BeSameAs(e);
        
        e.Adder.Kind.Should().Be(MethodKind.EventAdder);
        e.Remover.Kind.Should().Be(MethodKind.EventRemover);
        e.Accessors.Should().Equal(e.Adder, e.Remover);
    }
    
    [Theory]
    [TestData(nameof(TestData.Accessibility))]
    public void Event_Accessibility(string modifier, MetadataAccessibility expectedAccessibility)
    {
        var source = @$"
            class C {{
                {modifier} event System.EventHandler E;
            }}
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var e = a.Should().ContainSingleEvent("C.E").Subject;
        
        e.Accessibility.Should().Be(expectedAccessibility);
    }
    
    [Fact]
    public void Event_CustomAttribute()
    {
        var source = @"
            class C {
                [D]
                event System.EventHandler E;
            }
            class D : System.Attribute {}            
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var e = a.Should().ContainSingleEvent("C.E").Subject;

        var ca = e.GetCustomAttributes().Single();
        ca.Constructor.ContainingType.GetDocumentationId().Should().Be("T:D");
    }
}
