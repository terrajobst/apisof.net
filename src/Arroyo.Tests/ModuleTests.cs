namespace Arroyo.Tests;

public class ModuleTests
{
    [Fact]
    public void Module_ExportedTypes()
    {
        var source = @"
            using System;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            [assembly: TypeForwardedTo(typeof(Int32))]
        ";

        var module = CreateModule(source);

        var tf = module.ExportedTypes.Should().ContainSingle().Subject;

        tf.IsForwarder.Should().BeTrue();
        tf.Reference.ContainingFile.Name.Should().Be("System.Runtime");
        tf.Reference.GetFullName().Should().Be("System.Int32");
    }

    [Fact]
    public void Module_GetTypeReferences_References()
    {
        var source = @"
            class Bar { }
        ";

        var module = CreateModule(source);

        var typeReferences = module.GetTypeReferences().ToArray();
        var type = typeReferences.Should().ContainSingle(t => t.GetFullName() == "System.Object").Subject;

        type.ContainingType.Should().BeNull();
        type.ContainingFile.Name.Should().Be("System.Runtime");
    }

    [Fact]
    public void Module_GetTypeReferences_Nested()
    {
        var source = @"
            using System;

            class Foo {
                void Bar() {
                    var x = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                }
            }
        ";
      
        var module = CreateModule(source);

        var typeReferences = module.GetTypeReferences().ToArray();
        var specialFolder = typeReferences.Should().ContainSingle(t => t.GetFullName() == "System.Environment.SpecialFolder").Subject;

        specialFolder.ContainingType?.GetFullName().Should().Be("System.Environment");
        specialFolder.ContainingFile.Name.Should().Be("System.Runtime");
    }
    
    [Fact]
    public void Module_CustomAttributes()
    {
        var source = @"
            [module: Some]
            class SomeAttribute : System.Attribute { }            
        ";

        var module = CreateModule(source);
        module.GetCustomAttributes().Should().ContainSingle(x => x.Constructor.ContainingType.GetDocumentationId() == "T:SomeAttribute");
    }

    protected virtual MetadataModule CreateModule(string source)
    {
        return MetadataFactory.CreateModule(source);
    }
}