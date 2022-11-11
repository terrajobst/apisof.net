namespace Arroyo.Tests;

public sealed class CustomAttributeValueTests
{
    [Theory]
    [MemberData(nameof(GetEnumSingleTypes))]
    public void CustomAttributeValue_Enum_Single(string typeName, object value)
    {
        var source = @$"
            [Object({typeName}Enum.None)]
            class C {{}}
        ";

        var a = CreateAssemblyReferencingEnums(source);
        var t = a.Should().ContainSingleType("C").Subject;
        var ca = t.GetCustomAttributes().Should().ContainSingle().Subject;
        var arg = ca.FixedArguments.Should().ContainSingle().Subject;

        var type = arg.ValueType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;

        arg.IsArray.Should().BeFalse();
        type.Name.Should().Be(typeName + "Enum");
        arg.Value.Should().Be(value);
    }

    [Theory]
    [MemberData(nameof(GetEnumPairedTypes))]
    public void CustomAttributeValue_Enum_Array(string typeName1, string typeName2, object value1, object value2)
    {
        var source = @$"
            [Object(new object[] {{{typeName1}Enum.None, {typeName2}Enum.None }})]
            class C {{}}
        ";

        var a = CreateAssemblyReferencingEnums(source);
        var t = a.Should().ContainSingleType("C").Subject;
        var ca = t.GetCustomAttributes().Should().ContainSingle().Subject;
        var arg = ca.FixedArguments.Should().ContainSingle().Subject;

        arg.IsArray.Should().BeTrue();
        arg.Values.Length.Should().Be(2);

        var type1 = arg.Values[0].ValueType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        var type2 = arg.Values[1].ValueType.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        type1.Name.Should().Be(typeName1 + "Enum");
        type2.Name.Should().Be(typeName2 + "Enum");
        arg.Values.Select(tv => tv.Value).Should().Equal(value1, value2);
    }

    public static IEnumerable<object[]> GetEnumSingleTypes()
    {
        return GetUnderlyingEnumTypes().Select(t => new[] { t.TypeName, t.Value });
    }

    public static IEnumerable<object[]> GetEnumPairedTypes()
    {
        foreach (var (typeName1, value1) in GetUnderlyingEnumTypes())
            foreach (var (typeName2, value2) in GetUnderlyingEnumTypes())
                yield return new[] { typeName1, typeName2, value1, value2 };
    }

    private static IEnumerable<(string TypeName, object Value)> GetUnderlyingEnumTypes()
    {
        yield return ("Byte", (byte)42);
        yield return ("SByte", (byte)42);
        yield return ("Int16", (ushort)42);
        yield return ("UInt16", (ushort)42);
        yield return ("Int32", (uint)42);
        yield return ("UInt32", (uint)42);
        yield return ("Int64", (ulong)42);
        yield return ("UInt64", (ulong)42);
    }

    private static MetadataAssembly CreateAssemblyReferencingEnums(string source)
    {
        var enumSources = @"
            public enum ByteEnum : byte { None = 42 }
            public enum SByteEnum : byte { None = 42 }
            public enum Int16Enum : short { None = 42 }
            public enum UInt16Enum : short { None = 42 }
            public enum Int32Enum : int { None = 42 }
            public enum UInt32Enum : int { None = 42 }
            public enum Int64Enum : long { None = 42 }
            public enum UInt64Enum : long { None = 42 }

            public class ObjectAttribute : System.Attribute
            {
                public ObjectAttribute(object value) { }
            }
        ";

        var enumCompilation = MetadataFactory.CreateCompilation(enumSources);
        var compilation = MetadataFactory.CreateCompilation(source, new[] { enumCompilation });
        return compilation.ToMetadataAssembly();
    }

    [Fact]
    public void CustomAttributeValue_SystemType()
    {
        var source = @"            
            class ObjectAttribute : System.Attribute
            {
                public ObjectAttribute(object value) { }
            }
            [Object(typeof(C))]
            class C { } 
        ";

        var a = MetadataFactory.CreateAssembly(source);
        var c = a.Should().ContainSingleType("C").Subject;
        var ca = c.GetCustomAttributes().Should().ContainSingle().Subject;
        var arg = ca.FixedArguments.Should().ContainSingle().Subject;
        arg.ValueType.GetDocumentationId().Should().Be("T:System.Type");
        
        var value = arg.Value.Should().BeAssignableTo<MetadataNamedTypeReference>().Subject;
        value.GetDocumentationId().Should().Be("T:C");
    }
}
