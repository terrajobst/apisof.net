namespace Arroyo.Tests;

public sealed class InterfaceTests
{
    [Fact]
    public void Interface_Members_Abstract()
    {
        var source = @"
            interface I {
                void M();
                int P { get; }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var i = a.Should().ContainSingleType("I").Subject;
        var m = a.Should().ContainSingleMethod("I.M").Subject; 
        var p = i.GetProperties().Should().ContainSingle().Subject;
        
        m.Name.Should().Be("M");
        m.IsStatic.Should().BeFalse();
        m.IsAbstract.Should().BeTrue();
        
        p.Name.Should().Be("P");
        p.Getter!.IsStatic.Should().BeFalse();
        p.Getter!.IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void Interface_Members_Virtual()
    {
        var source = @"
            interface I {
                virtual void M() {}
                virtual int P => throw null;
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var i = a.Should().ContainSingleType("I").Subject;
        var m = a.Should().ContainSingleMethod("I.M").Subject; 
        var p = i.GetProperties().Should().ContainSingle().Subject;
        
        m.Name.Should().Be("M");
        m.IsStatic.Should().BeFalse();
        m.IsVirtual.Should().BeTrue();
        
        p.Name.Should().Be("P");
        p.Getter!.IsStatic.Should().BeFalse();
        p.Getter!.IsVirtual.Should().BeTrue();
    }

    [Fact]
    public void Interface_Members_Static()
    {
        var source = @"
            interface I {
                static void M() {}
                static int P => throw null;
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var i = a.Should().ContainSingleType("I").Subject;
        var m = a.Should().ContainSingleMethod("I.M").Subject; 
        var p = i.GetProperties().Should().ContainSingle().Subject;
        
        m.Name.Should().Be("M");
        m.IsStatic.Should().BeTrue();
        
        p.Name.Should().Be("P");
        p.Getter!.IsStatic.Should().BeTrue();
    }
    
    [Fact]
    public void Interface_Members_Static_Abstract()
    {
        var source = @"
            interface I {
                static abstract void M();
                static abstract int P { get; }
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var i = a.Should().ContainSingleType("I").Subject;
        var m = a.Should().ContainSingleMethod("I.M").Subject;
        var p = i.GetProperties().Should().ContainSingle().Subject;
        
        m.Name.Should().Be("M");
        m.IsStatic.Should().BeTrue();
        m.IsAbstract.Should().BeTrue();
        
        p.Name.Should().Be("P");
        p.Getter!.IsStatic.Should().BeTrue();
        p.Getter!.IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void Interface_Members_Static_Virtual()
    {
        var source = @"
            interface I {
                static virtual void M() {}
                static virtual int P => throw null;
            }
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var i = a.Should().ContainSingleType("I").Subject;
        var m = a.Should().ContainSingleMethod("I.M").Subject;
        var p = i.GetProperties().Should().ContainSingle().Subject;
        
        m.Name.Should().Be("M");
        m.IsStatic.Should().BeTrue();
        m.IsVirtual.Should().BeTrue();
        
        p.Name.Should().Be("P");
        p.Getter!.IsStatic.Should().BeTrue();
        p.Getter!.IsVirtual.Should().BeTrue();
    }
}