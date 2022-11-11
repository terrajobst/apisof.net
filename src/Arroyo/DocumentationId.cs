using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text;
using Arroyo.Signatures;

namespace Arroyo;

// Note: The implementation of this class follows the documentation of the format as defined here:
//
//       https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/
//
// Please note that there are differences between what is stated there and what Roslyn does today, comments explain
// any deviation.
//
// Specifically:
//
// - Roslyn doesn't seem to emit modifiers with ! (mod opt) and | (mod req) syntax
// - Roslyn doesn't provide a documentation ID for function pointers
// - Roslyn doesn't always include the return type for conversion operators unless they are valid conversion operators
//   in C#/VB. Specifically, non-static conversion operators as defined by C++/CLI won't include return types. 

internal static class DocumentationId
{
    public static string? Get(MetadataItem metadataItem)
    {
        switch (metadataItem)
        {
            case MetadataNamespace definition:
                return Get(definition);
            case MetadataType type:
                return Get(type);
            case MetadataFieldReference field:
                return Get(field);
            case MetadataMethodReference method:
                return Get(method);
            case MetadataProperty property:
                return Get(property);
            case MetadataEvent @event:
                return Get(@event);
            default:
                return null;
        }
    }

    private static string Get(this MetadataNamespace definition)
    {
        var builder = new StringBuilder();
        builder.Append("N:");
        builder.Append(definition.FullName);
        return builder.ToString();
    }

    private static string Get(this MetadataType type)
    {
        var builder = new StringBuilder();
        builder.Append("T:");
        builder.AppendType(type);
        return builder.ToString();
    }

    private static string Get(this MetadataFieldReference field)
    {
        var builder = new StringBuilder();
        builder.Append("F:");
        builder.AppendType(field.ContainingType);
        builder.Append('.');
        builder.AppendName(field.Name);
        return builder.ToString();
    }

    private static string Get(this MetadataMethodReference method)
    {
        var builder = new StringBuilder();
        builder.Append("M:");
        builder.AppendType(method.ContainingType);
        builder.Append('.');
        builder.AppendName(method.Name);

        if (method.Signature.GenericParameterCount > 0)
        {
            builder.Append("``");
            builder.Append(method.Signature.GenericParameterCount);
        }

        builder.AppendParameters(method.Signature);

        // NOTE: This method handles both references and definitions.
        //
        //       Roslyn will not include the return type unless it detects the method as a conversion operator, which
        //       in C#/VB semantics requires the method to be static, which other languages, such as C++/CLI may not.
        //
        //       We could replicate the Roslyn behavior in the case of method definitions, but this poses problems
        //       when looking at method references which would now result in a different documentation ID, which is
        //       problematic. Hence, I think assuming it's a conversion operator by name alone is less fragile.

        var isConversionOperator = method.Name is SpecialNames.ImplicitConversionName or
                                                  SpecialNames.ExplicitConversionName;

        if (isConversionOperator)
        {
            builder.Append('~');
            builder.AppendType(method.Signature.ReturnType);
        }

        if (method is MetadataMethodInstance methodInstance)
            builder.AppendTypeArguments(methodInstance.TypeArguments);

        return builder.ToString();
    }

    private static string Get(this MetadataProperty property)
    {
        var builder = new StringBuilder();
        builder.Append("P:");
        builder.AppendTypeReference(property.ContainingType);
        builder.Append('.');
        builder.AppendName(property.Name);
        builder.AppendParameters(property.Signature);
        return builder.ToString();
    }

    private static string Get(this MetadataEvent @event)
    {
        var builder = new StringBuilder();
        builder.Append("E:");
        builder.AppendTypeReference(@event.ContainingType);
        builder.Append('.');
        builder.AppendName(@event.Name);
        return builder.ToString();
    }

    private static void AppendType(this StringBuilder builder, MetadataType? type)
    {
        switch (type)
        {
            case MetadataNamedTypeInstance genericInstanceType:
                builder.AppendNamedTypeInstance(genericInstanceType);
                break;
            case MetadataArrayType arrayType:
                builder.AppendArrayType(arrayType);
                break;
            case MetadataByReferenceType byReferenceType:
                builder.AppendByReferenceType(byReferenceType);
                break;
            case MetadataPointerType pointerType:
                builder.AppendPointerType(pointerType);
                break;
            case MetadataFunctionPointerType functionPointerType:
                builder.AppendFunctionPointerType(functionPointerType);
                break;
            case MetadataTypeParameterReference typeParameterReference:
                builder.AppendTypeParameterReference(typeParameterReference);
                break;
            case MetadataModifiedType modifiedType:
                builder.AppendModifiedType(modifiedType);
                break;
            case MetadataPinnedType pinnedType:
                builder.AppendPinnedType(pinnedType);
                break;
            case MetadataNamedTypeReference typeReference:
                builder.AppendTypeReference(typeReference);
                break;
            default:
                throw new Exception($"Unexpected type {type}");
        }
    }

    private static void AppendNamedTypeInstance(this StringBuilder builder, MetadataNamedTypeInstance type)
    {
        if (type.ContainingType is not null)
        {
            builder.AppendType(type.ContainingType);
            builder.Append('.');
        }
        else if (type.NamespaceName.Length > 0)
        {
            builder.Append(type.NamespaceName);
            builder.Append('.');
        }

        builder.AppendName(type.Name);

        if (type.TypeArguments.Length > 0)
        {
            builder.Append('{');
            var isFirst = true;

            foreach (var argument in type.TypeArguments)
            {
                if (isFirst)
                    isFirst = false;
                else
                    builder.Append(',');

                builder.AppendType(argument);
            }

            builder.Append('}');
        }
    }

    private static void AppendArrayType(this StringBuilder builder, MetadataArrayType type)
    {
        builder.AppendType(type.ElementType);
        builder.Append('[');

        for (var i = 0; i < type.Rank; i++)
        {
            if (i > 0)
                builder.Append(',');

            builder.Append(type.LowerBounds[i]);
            builder.Append(':');

            if (i < type.Sizes.Length)
                builder.Append(type.Sizes[i]);
        }

        builder.Append(']');
    }

    private static void AppendByReferenceType(this StringBuilder builder, MetadataByReferenceType type)
    {
        builder.AppendType(type.ElementType);
        builder.Append('@');
    }

    private static void AppendPointerType(this StringBuilder builder, MetadataPointerType type)
    {
        builder.AppendType(type.ElementType);
        builder.Append('*');
    }

    private static void AppendFunctionPointerType(this StringBuilder builder, MetadataFunctionPointerType type)
    {
        builder.Append("function ");
        builder.AppendType(type.Signature.ReturnType);
        builder.Append(" (");

        var isFirst = true;

        foreach (var parameter in type.Signature.Parameters)
        {
            if (isFirst)
                isFirst = false;
            else
                builder.Append(',');

            builder.AppendParameter(parameter);
        }

        builder.Append(')');
    }

    private static void AppendTypeParameterReference(this StringBuilder builder, MetadataTypeParameterReference type)
    {
        builder.Append('`');
        if (!type.IsType)
            builder.Append('`');
        builder.Append(type.Index);
    }

    private static void AppendModifiedType(this StringBuilder builder, MetadataModifiedType type)
    {
        builder.AppendType(type.UnmodifiedType);

        // Note: It seems Roslyn doesn't include modifiers in the ID. However, modifiers will result in different
        //       methods and the CLR allows overloading between them. Hence, it seems unwise to exclude the modifiers
        //       from the documentation ID. Hence we'll always output them.

        var marker = type.CustomModifier.IsRequired ? "!" : "|";
        builder.Append(marker);
        builder.AppendType(type.CustomModifier.Type);
    }

    private static void AppendPinnedType(this StringBuilder builder, MetadataPinnedType type)
    {
        builder.AppendType(type.ElementType);
        builder.Append('^');
    }

    private static void AppendTypeReference(this StringBuilder builder, MetadataNamedTypeReference type)
    {
        var needsDot = true;

        if (type.ContainingType is not null)
        {
            builder.AppendTypeReference(type.ContainingType);
        }
        else
        {
            builder.Append(type.NamespaceName);
            needsDot = !string.IsNullOrEmpty(type.NamespaceName);
        }

        if (needsDot)
            builder.Append('.');

        builder.AppendName(type.Name);

        if (type.GenericArity > 0)
        {
            builder.Append('`');
            builder.Append(type.GenericArity);
        }
    }

    private static void AppendName(this StringBuilder builder, string name)
    {
        foreach (var c in name)
        {
            if (c == '.')
                builder.Append('#');
            else if (c == '<')
                builder.Append('{');
            else if (c == '>')
                builder.Append('}');
            else if (c == ',')
                builder.Append('@');
            else
                builder.Append(c);
        }
    }

    private static void AppendParameters(this StringBuilder builder, MetadataSignature signature)
    {
        // CCI and Roslyn differ how they produce __arglist documentation IDs.
        //
        // C# simply emits them as an empty string, but it's treated as an extra
        // parameter. Let's consider these methods:
        //
        //        class C {
        //            void M1(__arglist)
        //            void M2(int x, __arglist)
        //        }
        //
        // Here is what they produce:
        //
        //        C#                    | CCI
        //        ----------------------|-------------------------------
        //        M:C.M1()              | M:C.M1
        //        M:C.M2(System.Int32,) | M:C.M2(System.Int32,__arglist)
        //
        // It seems CCI has an issue where if the method takes no parameters,
        // __arglist is entirely dropped on the floor, but otherwise it is
        // rendered as `__arglist`.
        //
        // C# on the other hand treats is as a parameter, but will emit it
        // as a blank value.

        var hasVarArgs = signature.CallingConvention == SignatureCallingConvention.VarArgs;

        if (!signature.Parameters.Any() && !hasVarArgs)
            return;

        builder.Append('(');

        var isFirst = true;
        foreach (var parameter in signature.Parameters)
        {
            if (isFirst)
                isFirst = false;
            else
                builder.Append(',');

            builder.AppendParameter(parameter);
        }

        if (hasVarArgs)
        {
            if (!isFirst)
                builder.Append(',');
        }

        builder.Append(')');
    }

    private static void AppendParameter(this StringBuilder builder, MetadataSignatureParameter parameter)
    {
        builder.AppendType(parameter.ToRawType());
    }

    private static void AppendTypeArguments(this StringBuilder builder, ImmutableArray<MetadataType> typeArguments)
    {
        builder.Append('{');
        var isFirst = true;
        foreach (var typeArgument in typeArguments)
        {
            if (isFirst)
                isFirst = false;
            else
                builder.Append(',');
            builder.AppendType(typeArgument);
        }

        builder.Append('}');
    }
}