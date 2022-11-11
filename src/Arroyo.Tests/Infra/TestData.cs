namespace Arroyo.Tests.Infra;

public static class TestData
{
    public static IEnumerable<(string Name, MetadataAccessibility Value)> Accessibility { get; } = new[]
    {
        ("public", MetadataAccessibility.Public),
        ("protected", MetadataAccessibility.Family),
        ("private", MetadataAccessibility.Private),
        ("internal", MetadataAccessibility.Assembly),
        ("protected internal", MetadataAccessibility.FamilyOrAssembly),
        ("private protected", MetadataAccessibility.FamilyAndAssembly),
    };

    public static IEnumerable<(string MethodName, MethodKind Kind)> OperatorMethods { get; } = new[]
    {
        ("op_Implicit", MethodKind.ImplicitConversion),
        ("op_Explicit", MethodKind.ExplicitConversion),
        ("op_Addition", MethodKind.AdditionOperator),
        ("op_BitwiseAnd", MethodKind.BitwiseAndOperator),
        ("op_BitwiseOr", MethodKind.BitwiseOrOperator),
        ("op_Decrement", MethodKind.DecrementOperator),
        ("op_Division", MethodKind.DivisionOperator),
        ("op_Equality", MethodKind.EqualityOperator),
        ("op_ExclusiveOr", MethodKind.ExclusiveOrOperator),
        ("op_False", MethodKind.FalseOperator),
        ("op_GreaterThan", MethodKind.GreaterThanOperator),
        ("op_GreaterThanOrEqual", MethodKind.GreaterThanOrEqualOperator),
        ("op_Increment", MethodKind.IncrementOperator),
        ("op_Inequality", MethodKind.InequalityOperator),
        ("op_LeftShift", MethodKind.LeftShiftOperator),
        ("op_LessThan", MethodKind.LessThanOperator),
        ("op_LessThanOrEqual", MethodKind.LessThanOrEqualOperator),
        ("op_Modulus", MethodKind.ModulusOperator),
        ("op_Multiply", MethodKind.MultiplyOperator),
        ("op_OnesComplement", MethodKind.OnesComplementOperator),
        ("op_RightShift", MethodKind.RightShiftOperator),
        ("op_Subtraction", MethodKind.SubtractionOperator),
        ("op_True", MethodKind.TrueOperator),
        ("op_UnaryNegation", MethodKind.UnaryNegationOperator),
        ("op_UnaryPlus", MethodKind.UnaryPlusOperator),

        // TODO: These operators arent' supported by C#
        //
        // ("op_LogicalNot", MethodKind.LogicalNotOperator),
        // ("op_LogicalOr", MethodKind.LogicalOrOperator),
        // ("op_LogicalAnd", MethodKind.LogicalAndOperator),
        // ("op_Exponent", MethodKind.ExponentOperator),
        // ("op_Concatenate", MethodKind.ConcatenateOperator),
        // ("op_UnsignedLeftShift", MethodKind.UnsignedLeftShiftOperator),
        // ("op_UnsignedRightShift", MethodKind.UnsignedRightShiftOperator),
        // ("op_IntegerDivision", MethodKind.IntegerDivisionOperator),
        // ("op_Like", MethodKind.LikeOperator),        
    };
}