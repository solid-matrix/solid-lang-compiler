namespace SolidLangCompiler.SemanticTree;

/// <summary>
/// Base class for all semantic expressions.
/// Every expression has a known type after semantic analysis.
/// </summary>
public abstract record SemaExpression : SemaNode
{
    /// <summary>
    /// The type of this expression (always set after semantic analysis).
    /// </summary>
    public required SemaType Type { get; init; }
}

/// <summary>
/// Represents a local variable reference.
/// </summary>
public record SemaLocalRef : SemaExpression
{
    public required string Name { get; init; }
    public required int Index { get; init; } // Index in local variable table
    public override string ToString() => Name;
}

/// <summary>
/// Represents a parameter reference.
/// </summary>
public record SemaParamRef : SemaExpression
{
    public required string Name { get; init; }
    public required int Index { get; init; } // Parameter index
    public override string ToString() => Name;
}

/// <summary>
/// Represents a global variable/constant reference.
/// </summary>
public record SemaGlobalRef : SemaExpression
{
    public required string Name { get; init; }
    public required string MangledName { get; init; } // For code generation
    public override string ToString() => Name;
}

/// <summary>
/// Represents a function reference (for function pointers).
/// </summary>
public record SemaFuncRef : SemaExpression
{
    public required string Name { get; init; }
    public required string MangledName { get; init; }
    public override string ToString() => Name;
}

/// <summary>
/// Integer literal expression.
/// </summary>
public record SemaIntLiteral : SemaExpression
{
    public required ulong Value { get; init; }
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Floating-point literal expression.
/// </summary>
public record SemaFloatLiteral : SemaExpression
{
    public required double Value { get; init; }
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Boolean literal expression.
/// </summary>
public record SemaBoolLiteral : SemaExpression
{
    public required bool Value { get; init; }
    public override string ToString() => Value ? "true" : "false";
}

/// <summary>
/// String literal expression.
/// </summary>
public record SemaStringLiteral : SemaExpression
{
    public required string Value { get; init; }
    public override string ToString() => $"\"{Value}\"";
}

/// <summary>
/// Null literal expression.
/// </summary>
public record SemaNullLiteral : SemaExpression
{
    public override string ToString() => "null";
}

/// <summary>
/// Binary expression (a + b, a == b, etc.).
/// </summary>
public record SemaBinaryExpr : SemaExpression
{
    public required SemaExpression Left { get; init; }
    public required SemaBinaryOp Operator { get; init; }
    public required SemaExpression Right { get; init; }
    public override string ToString() => $"({Left} {Operator.Symbol()} {Right})";
}

/// <summary>
/// Binary operators.
/// </summary>
public enum SemaBinaryOp
{
    // Arithmetic
    Add, Subtract, Multiply, Divide, Modulo,
    // Comparison
    Equal, NotEqual, Less, Greater, LessEqual, GreaterEqual,
    // Logical
    LogicalAnd, LogicalOr,
    // Bitwise
    BitwiseAnd, BitwiseOr, BitwiseXor,
    // Shift
    ShiftLeft, ShiftRight
}

public static class SemaBinaryOpExtensions
{
    public static string Symbol(this SemaBinaryOp op) => op switch
    {
        SemaBinaryOp.Add => "+",
        SemaBinaryOp.Subtract => "-",
        SemaBinaryOp.Multiply => "*",
        SemaBinaryOp.Divide => "/",
        SemaBinaryOp.Modulo => "%",
        SemaBinaryOp.Equal => "==",
        SemaBinaryOp.NotEqual => "!=",
        SemaBinaryOp.Less => "<",
        SemaBinaryOp.Greater => ">",
        SemaBinaryOp.LessEqual => "<=",
        SemaBinaryOp.GreaterEqual => ">=",
        SemaBinaryOp.LogicalAnd => "&&",
        SemaBinaryOp.LogicalOr => "||",
        SemaBinaryOp.BitwiseAnd => "&",
        SemaBinaryOp.BitwiseOr => "|",
        SemaBinaryOp.BitwiseXor => "^",
        SemaBinaryOp.ShiftLeft => "<<",
        SemaBinaryOp.ShiftRight => ">>",
        _ => throw new ArgumentOutOfRangeException()
    };
}

/// <summary>
/// Unary expression (-a, !a, *a, &amp;a, etc.).
/// </summary>
public record SemaUnaryExpr : SemaExpression
{
    public required SemaUnaryOp Operator { get; init; }
    public required SemaExpression Operand { get; init; }
    public override string ToString() => $"({Operator.Symbol()}{Operand})";
}

/// <summary>
/// Unary operators.
/// </summary>
public enum SemaUnaryOp
{
    Negate,         // -
    LogicalNot,     // !
    BitwiseNot,     // ~
    AddressOf,      // &
    Dereference,    // *
    Ref,            // ^
    MutRef          // ^!
}

public static class SemaUnaryOpExtensions
{
    public static string Symbol(this SemaUnaryOp op) => op switch
    {
        SemaUnaryOp.Negate => "-",
        SemaUnaryOp.LogicalNot => "!",
        SemaUnaryOp.BitwiseNot => "~",
        SemaUnaryOp.AddressOf => "&",
        SemaUnaryOp.Dereference => "*",
        SemaUnaryOp.Ref => "^",
        SemaUnaryOp.MutRef => "^!",
        _ => throw new ArgumentOutOfRangeException()
    };
}

/// <summary>
/// Function call expression.
/// </summary>
public record SemaCallExpr : SemaExpression
{
    public required SemaExpression Target { get; init; }
    public required IReadOnlyList<SemaExpression> Arguments { get; init; }
    public override string ToString()
    {
        var args = string.Join(", ", Arguments);
        return $"{Target}({args})";
    }
}

/// <summary>
/// Field access expression (obj.field).
/// </summary>
public record SemaFieldAccessExpr : SemaExpression
{
    public required SemaExpression Target { get; init; }
    public required string FieldName { get; init; }
    public required int FieldIndex { get; init; }
    public override string ToString() => $"{Target}.{FieldName}";
}

/// <summary>
/// Index expression (arr[index]).
/// </summary>
public record SemaIndexExpr : SemaExpression
{
    public required SemaExpression Target { get; init; }
    public required SemaExpression Index { get; init; }
    public override string ToString() => $"{Target}[{Index}]";
}

/// <summary>
/// Conditional expression (cond ? then : else).
/// </summary>
public record SemaConditionalExpr : SemaExpression
{
    public required SemaExpression Condition { get; init; }
    public required SemaExpression ThenExpr { get; init; }
    public required SemaExpression ElseExpr { get; init; }
    public override string ToString() => $"({Condition} ? {ThenExpr} : {ElseExpr})";
}

/// <summary>
/// Cast expression (expr as Type).
/// </summary>
public record SemaCastExpr : SemaExpression
{
    public required SemaExpression Operand { get; init; }
    public required SemaType SourceType { get; init; }
    public override string ToString() => $"({Operand} as {Type})";
}

/// <summary>
/// Array literal expression [T]{elem1, elem2, ...}.
/// </summary>
public record SemaArrayLiteral : SemaExpression
{
    public required IReadOnlyList<SemaExpression> Elements { get; init; }
    public override string ToString()
    {
        var elems = string.Join(", ", Elements);
        return $"[{elems}]";
    }
}

/// <summary>
/// Struct literal expression Type{field1 = value1, field2 = value2, ...}.
/// </summary>
public record SemaStructLiteral : SemaExpression
{
    public required string TypeName { get; init; }
    public required IReadOnlyList<SemaStructLiteralField> Fields { get; init; }
    public override string ToString()
    {
        var fields = string.Join(", ", Fields);
        return $"{TypeName}{{{fields}}}";
    }
}

/// <summary>
/// Represents a field in a struct literal.
/// </summary>
public record SemaStructLiteralField(string Name, SemaExpression Value)
{
    public override string ToString() => $"{Name} = {Value}";
}

/// <summary>
/// Union literal expression Type::field(value).
/// </summary>
public record SemaUnionLiteral : SemaExpression
{
    public required SemaUnionType UnionType { get; init; }
    public required string FieldName { get; init; }
    public required SemaExpression Value { get; init; }
    public override string ToString() => $"{UnionType.Name}::{FieldName}({Value})";
}
