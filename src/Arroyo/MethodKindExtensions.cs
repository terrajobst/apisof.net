namespace Arroyo;

public static class MethodKindExtensions
{
    public static bool IsConstructorOrClassConstructor(this MethodKind kind)
    {
        switch (kind)
        {
            case MethodKind.Constructor:
            case MethodKind.ClassConstructor:
                return true;
            default:
                return false;
        }
    }

    public static bool IsAccessor(this MethodKind kind)
    {
        switch (kind)
        {
            case MethodKind.EventAdder:
            case MethodKind.EventRaiser:
            case MethodKind.EventRemover:
            case MethodKind.PropertyGetter:
            case MethodKind.PropertySetter:
                return true;
            default:
                return false;
        }
    }

    public static bool IsConversionOperator(this MethodKind kind)
    {
        switch (kind)
        {
            case MethodKind.ImplicitConversion:
            case MethodKind.ExplicitConversion:
                return true;
            default:
                return false;
        }
    }

    public static bool IsOperator(this MethodKind kind)
    {
        switch (kind)
        {
            case MethodKind.ImplicitConversion:
            case MethodKind.ExplicitConversion:
            case MethodKind.AdditionOperator:
            case MethodKind.BitwiseAndOperator:
            case MethodKind.BitwiseOrOperator:
            case MethodKind.DecrementOperator:
            case MethodKind.DivisionOperator:
            case MethodKind.EqualityOperator:
            case MethodKind.ExclusiveOrOperator:
            case MethodKind.FalseOperator:
            case MethodKind.GreaterThanOperator:
            case MethodKind.GreaterThanOrEqualOperator:
            case MethodKind.IncrementOperator:
            case MethodKind.InequalityOperator:
            case MethodKind.LeftShiftOperator:
            case MethodKind.UnsignedLeftShiftOperator:
            case MethodKind.LessThanOperator:
            case MethodKind.LessThanOrEqualOperator:
            case MethodKind.LogicalNotOperator:
            case MethodKind.LogicalOrOperator:
            case MethodKind.LogicalAndOperator:
            case MethodKind.ModulusOperator:
            case MethodKind.MultiplyOperator:
            case MethodKind.OnesComplementOperator:
            case MethodKind.RightShiftOperator:
            case MethodKind.UnsignedRightShiftOperator:
            case MethodKind.SubtractionOperator:
            case MethodKind.TrueOperator:
            case MethodKind.UnaryNegationOperator:
            case MethodKind.UnaryPlusOperator:
            case MethodKind.ConcatenateOperator:
            case MethodKind.ExponentOperator:
            case MethodKind.IntegerDivisionOperator:
            case MethodKind.LikeOperator:
                return true;
            default:
                return false;
        }
    }
}