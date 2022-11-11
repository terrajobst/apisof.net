namespace Arroyo;

internal static class SpecialNames
{
    public const string ConstructorName = ".ctor";
    public const string ClassConstructorName = ".cctor";
    public const string FinalizeName = "Finalize";

    public const string DelegateInvokeName = "Invoke";

    public const string ImplicitConversionName = "op_Implicit";
    public const string ExplicitConversionName = "op_Explicit";
    public const string AdditionOperatorName = "op_Addition";
    public const string BitwiseAndOperatorName = "op_BitwiseAnd";
    public const string BitwiseOrOperatorName = "op_BitwiseOr";
    public const string DecrementOperatorName = "op_Decrement";
    public const string DivisionOperatorName = "op_Division";
    public const string EqualityOperatorName = "op_Equality";
    public const string ExclusiveOrOperatorName = "op_ExclusiveOr";
    public const string FalseOperatorName = "op_False";
    public const string GreaterThanOperatorName = "op_GreaterThan";
    public const string GreaterThanOrEqualOperatorName = "op_GreaterThanOrEqual";
    public const string IncrementOperatorName = "op_Increment";
    public const string InequalityOperatorName = "op_Inequality";
    public const string LeftShiftOperatorName = "op_LeftShift";
    public const string UnsignedLeftShiftOperatorName = "op_UnsignedLeftShift";
    public const string LessThanOperatorName = "op_LessThan";
    public const string LessThanOrEqualOperatorName = "op_LessThanOrEqual";
    public const string LogicalNotOperatorName = "op_LogicalNot";
    public const string LogicalOrOperatorName = "op_LogicalOr";
    public const string LogicalAndOperatorName = "op_LogicalAnd";
    public const string ModulusOperatorName = "op_Modulus";
    public const string MultiplyOperatorName = "op_Multiply";
    public const string OnesComplementOperatorName = "op_OnesComplement";
    public const string RightShiftOperatorName = "op_RightShift";
    public const string UnsignedRightShiftOperatorName = "op_UnsignedRightShift";
    public const string SubtractionOperatorName = "op_Subtraction";
    public const string TrueOperatorName = "op_True";
    public const string UnaryNegationOperatorName = "op_UnaryNegation";
    public const string UnaryPlusOperatorName = "op_UnaryPlus";
    public const string ConcatenateOperatorName = "op_Concatenate";
    public const string ExponentOperatorName = "op_Exponent";
    public const string IntegerDivisionOperatorName = "op_IntegerDivision";
    public const string LikeOperatorName = "op_Like";

    public const string SystemValueType = "System.ValueType";
    public const string SystemEnum = "System.Enum";
    public const string SystemMulticastDelegate = "System.MulticastDelegate";

    public static string GetTypeName(string metadataName)
    {
        return GetTypeName(metadataName, out _);
    }

    public static string GetTypeName(string metadataName, out int genericArity)
    {
        // Generic types are suffixed with a backtick and the number of
        // type parameters
        var index = metadataName.IndexOf('`');
        if (index < 0)
        {
            genericArity = 0;
            return metadataName;
        }
        else
        {
            if (!int.TryParse(metadataName.AsSpan(index + 1), out genericArity))
                return metadataName;

            return metadataName.Substring(0, index);
        }
    }

    public static string GetMethodName(string metadataName, out int genericArity)
    {
        // Generic methods are suffixed with two backticks and the number of
        // type parameters
        var index = metadataName.IndexOf("``", StringComparison.Ordinal);
        if (index < 0)
        {
            genericArity = 0;
            return metadataName;
        }
        else
        {
            if (!int.TryParse(metadataName.AsSpan(index + 2), out genericArity))
                return metadataName;

            return metadataName.Substring(0, index);
        }
    }

    public static MetadataSpecialType? GetSpecialTypeForName(string name)
    {
        switch (name)
        {
            case "Object":
                return MetadataSpecialType.System_Object;
            case "Boolean":
                return MetadataSpecialType.System_Boolean;
            case "Byte":
                return MetadataSpecialType.System_Byte;
            case "SByte":
                return MetadataSpecialType.System_SByte;
            case "Char":
                return MetadataSpecialType.System_Char;
            case "Int16":
                return MetadataSpecialType.System_Int16;
            case "UInt16":
                return MetadataSpecialType.System_UInt16;
            case "Int32":
                return MetadataSpecialType.System_Int32;
            case "UInt32":
                return MetadataSpecialType.System_UInt32;
            case "Int64":
                return MetadataSpecialType.System_Int64;
            case "UInt64":
                return MetadataSpecialType.System_UInt64;
            case "Single":
                return MetadataSpecialType.System_Single;
            case "Double":
                return MetadataSpecialType.System_Double;
            case "IntPtr":
                return MetadataSpecialType.System_IntPtr;
            case "UIntPtr":
                return MetadataSpecialType.System_UIntPtr;
            case "String":
                return MetadataSpecialType.System_String;
            case "Type":
                return MetadataSpecialType.System_Type;
            case "TypedReference":
                return MetadataSpecialType.System_TypedReference;
            case "Void":
                return MetadataSpecialType.System_Void;
            default:
                return null;
        }
    }

    public static string GetNameForSpecialType(MetadataSpecialType specialType)
    {
        switch (specialType)
        {
            case MetadataSpecialType.System_Object:
                return "Object";
            case MetadataSpecialType.System_Boolean:
                return "Boolean";
            case MetadataSpecialType.System_Byte:
                return "Byte";
            case MetadataSpecialType.System_SByte:
                return "SByte";
            case MetadataSpecialType.System_Char:
                return "Char";
            case MetadataSpecialType.System_Int16:
                return "Int16";
            case MetadataSpecialType.System_UInt16:
                return "UInt16";
            case MetadataSpecialType.System_Int32:
                return "Int32";
            case MetadataSpecialType.System_UInt32:
                return "UInt32";
            case MetadataSpecialType.System_Int64:
                return "Int64";
            case MetadataSpecialType.System_UInt64:
                return "UInt64";
            case MetadataSpecialType.System_Single:
                return "Single";
            case MetadataSpecialType.System_Double:
                return "Double";
            case MetadataSpecialType.System_IntPtr:
                return "IntPtr";
            case MetadataSpecialType.System_UIntPtr:
                return "UIntPtr";
            case MetadataSpecialType.System_String:
                return "String";
            case MetadataSpecialType.System_Type:
                return "Type";
            case MetadataSpecialType.System_TypedReference:
                return "TypedReference";
            case MetadataSpecialType.System_Void:
                return "Void";
            default:
                throw new ArgumentOutOfRangeException(nameof(specialType), specialType, null);
        }
    }
}