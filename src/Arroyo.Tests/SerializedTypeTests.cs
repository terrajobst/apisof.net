using Arroyo.Signatures;

namespace Arroyo.Tests;

public sealed class SerializedTypeTests
{
    [Fact]
    public void SerializedType_Named()
    {
        var type = Create<MetadataNamedTypeReference>("typeof(string)");
        
        type.ContainingFile.Name.Should().Be("System.Runtime");
        type.NamespaceName.Should().Be("System");
        type.Name.Should().Be("String");
    }

    [Fact]
    public void SerializedType_Nested()
    {
        var innerType = Create<MetadataNamedTypeReference>("typeof(Test.Outer.Inner)");
        innerType.NamespaceName.Should().Be("Test");
        innerType.Name.Should().Be("Inner");

        var outerType = innerType.ContainingType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        outerType.NamespaceName.Should().Be("Test");
        outerType.Name.Should().Be("Outer");
        outerType.ContainingType.Should().BeNull();
    }

    [Fact]
    public void SerializedType_Generic()
    {
        var list = Create<MetadataNamedTypeReference>("typeof(System.Collections.Generic.List<>)");

        list.ContainingFile.Name.Should().Be("System.Collections");
        list.NamespaceName.Should().Be("System.Collections.Generic");
        list.Name.Should().Be("List");
        list.GenericArity.Should().Be(1);
    }

    [Fact]
    public void SerializedType_Instance()
    {
        var type = Create<MetadataNamedTypeInstance>("typeof(System.Collections.Generic.List<int>)");

        var int32 = type.TypeArguments.Should().ContainSingle().Subject.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        
        int32.ContainingFile.Name.Should().Be("System.Runtime");
        int32.NamespaceName.Should().Be("System");
        int32.Name.Should().Be("Int32");

        var list = type.GenericType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        list.ContainingFile.Name.Should().Be("System.Collections");
        list.NamespaceName.Should().Be("System.Collections.Generic");
        list.Name.Should().Be("List");
        list.GenericArity.Should().Be(1);
    }

    [Fact]
    public void SerializedType_Pointer()
    {
        var type = Create<MetadataPointerType>("typeof(int*)");

        var int32 = type.ElementType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        
        int32.ContainingFile.Name.Should().Be("System.Runtime");
        int32.NamespaceName.Should().Be("System");
        int32.Name.Should().Be("Int32");
    }

    [Fact]
    public void SerializedType_Array()
    {
        var type = Create<MetadataArrayType>("typeof(int[])");
        type.Rank.Should().Be(0);
        type.LowerBounds.Should().BeEmpty();
        type.Sizes.Should().BeEmpty();
        
        var int32 = type.ElementType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        
        int32.ContainingFile.Name.Should().Be("System.Runtime");
        int32.NamespaceName.Should().Be("System");
        int32.Name.Should().Be("Int32");
    }

    [Fact]
    public void SerializedType_Array_TwoDimensions()
    {
        var type = Create<MetadataArrayType>("typeof(int[,])");
        type.Rank.Should().Be(2);
        type.LowerBounds.Should().BeEmpty();
        type.Sizes.Should().BeEmpty();
        
        var int32 = type.ElementType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        
        int32.ContainingFile.Name.Should().Be("System.Runtime");
        int32.NamespaceName.Should().Be("System");
        int32.Name.Should().Be("Int32");
    }

    [Fact]
    public void SerializedType_Array_ThreeDimensions()
    {
        var type = Create<MetadataArrayType>("typeof(int[,,])");
        type.Rank.Should().Be(3);
        type.LowerBounds.Should().BeEmpty();
        type.Sizes.Should().BeEmpty();
        
        var int32 = type.ElementType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        
        int32.ContainingFile.Name.Should().Be("System.Runtime");
        int32.NamespaceName.Should().Be("System");
        int32.Name.Should().Be("Int32");
    }

    [Fact]
    public void SerializedType_Compound()
    {
        var array = Create<MetadataArrayType>("typeof(Test.Outer<int>.Inner<byte>*[])");
        array.Rank.Should().Be(0);
        array.LowerBounds.Should().BeEmpty();
        array.Sizes.Should().BeEmpty();

        var pointer = array.ElementType.Should().BeAssignableTo<MetadataPointerType>().Subject;

        var innerInstance = pointer.ElementType.Should().BeAssignableTo<MetadataNamedTypeInstance>().Subject;
        var innerArgument = innerInstance.TypeArguments.Should().ContainSingle().Subject;

        var innerType = innerInstance.GenericType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        innerType.NamespaceName.Should().Be("Test");
        innerType.Name.Should().Be("Inner");
        innerType.GenericArity.Should().Be(1);

        var byteType = innerArgument.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        byteType.NamespaceName.Should().Be("System");
        byteType.Name.Should().Be("Byte");
        byteType.GenericArity.Should().Be(0);
        byteType.ContainingType.Should().BeNull();

        var outerInstance = innerInstance.ContainingType.Should().BeAssignableTo<MetadataNamedTypeInstance>().Subject;
        var outerArgument = outerInstance.TypeArguments.Should().ContainSingle().Subject;

        var int32Type = outerArgument.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        int32Type.NamespaceName.Should().Be("System");
        int32Type.Name.Should().Be("Int32");
        int32Type.GenericArity.Should().Be(0);
        int32Type.ContainingType.Should().BeNull();

        var outerType = outerInstance.GenericType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        outerType.NamespaceName.Should().Be("Test");
        outerType.Name.Should().Be("Outer");
        outerType.GenericArity.Should().Be(1);
        outerType.ContainingType.Should().BeNull();
    }

    private static T Create<T>(string typeofContent)
    {
        var source = @$"
            namespace Test
            {{
                public struct Outer<K>
                    where K: unmanaged
                {{
                    public struct Inner<T>
                        where T: unmanaged
                    {{
                        public T Data;
                    }}
                }}

                public class Outer
                {{
                    public class Inner
                    {{
                    }}
                }}

            }}

            [System.ComponentModel.DefaultValue({typeofContent})]
            class Bar {{ }}
        ";

        var assembly = MetadataFactory.CreateAssembly(source);
        var bar = assembly.Should().ContainSingleType("Bar").Subject;
        var customAttribute = bar.GetCustomAttributes().Should().ContainSingle().Subject;
        var constructorParameter = customAttribute.FixedArguments.First();
        return constructorParameter.Value.Should().BeAssignableTo<T>().Subject;
    }
}