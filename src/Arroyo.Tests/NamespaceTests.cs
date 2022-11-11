namespace Arroyo.Tests;

public sealed class NamespaceTests
{
    [Fact]
    public void Namespace_Empty()
    {
        var assembly = MetadataFactory.CreateAssembly(string.Empty);

        assembly.MainModule.NamespaceRoot.Name.Should().Be(string.Empty);
        assembly.MainModule.NamespaceRoot.FullName.Should().Be(string.Empty);
        assembly.MainModule.NamespaceRoot.GetNamespaces().Should().BeEmpty();
        assembly.MainModule.NamespaceRoot.Parent.Should().BeNull();
    }

    [Fact]
    public void Namespace_Single()
    {
        var source = @"
            namespace Foo;
            class Test {}
        ";

        var assembly = MetadataFactory.CreateAssembly(source);

        var foo = assembly.MainModule.NamespaceRoot.GetNamespaces().Should().ContainSingle().Subject;
        foo.Name.Should().Be("Foo");
        foo.FullName.Should().Be("Foo");
        foo.Parent.Should().BeSameAs(assembly.MainModule.NamespaceRoot);

        var test = foo.GetTypes().Should().ContainSingle().Subject;
        test.Name.Should().Be("Test");
    }

    [Fact]
    public void Namespace_Nested()
    {
        var source = @"
            namespace Foo.Bar;
            class Test {}
        ";

        var assembly = MetadataFactory.CreateAssembly(source);

        assembly.MainModule.NamespaceRoot.Name.Should().Be(string.Empty);

        var foo = assembly.MainModule.NamespaceRoot.GetNamespaces().Should().ContainSingle().Subject;
        var bar = foo.GetNamespaces().Should().ContainSingle().Subject;

        foo.Name.Should().Be("Foo");
        foo.FullName.Should().Be("Foo");
        foo.GetTypes().Should().BeEmpty();
        foo.Parent.Should().BeSameAs(assembly.MainModule.NamespaceRoot);

        bar.Name.Should().Be("Bar");
        bar.FullName.Should().Be("Foo.Bar");
        bar.Parent.Should().BeSameAs(foo);
        bar.GetTypes().Should().ContainSingle();
    }
}
