using System.Reflection;
using System.Runtime.CompilerServices;

namespace Arroyo.Tests.Infra;

public sealed class TestDataAttribute : MemberDataAttributeBase
{
    public TestDataAttribute(string memberName, params object[] parameters)
        : base(memberName, parameters)
    {
        MemberType = typeof(TestData);
    }

    protected override object?[]? ConvertDataItem(MethodInfo testMethod, object? item)
    {
        if (item is null)
            return null;
        
        if (item is ITuple tuple)
        {
            var result = new object?[tuple.Length];
            for (var i = 0; i < result.Length; i++)
                result[i] = tuple[i];
            
            return result;
        }

        if (item is object[] array)
            return array;

        throw new ArgumentException($"Method {testMethod} yield an item that was neither a tuple nor an object[]. It was: {item}");
    }
}