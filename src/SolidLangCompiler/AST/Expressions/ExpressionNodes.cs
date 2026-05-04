using SolidLangCompiler.AST.Types;

namespace SolidLangCompiler.AST.Expressions;

/// <summary>
/// Base class for all expression nodes.
/// </summary>
public abstract record ExpressionNode : AstNode;

/// <summary>
/// Represents a binary expression (a + b, a && b, etc.).
/// </summary>
public record BinaryExpressionNode(ExpressionNode Left, BinaryOperator Operator, ExpressionNode Right) : ExpressionNode
{
    public override string ToString() => $"({Left} {Operator.Symbol()} {Right})";
}

public enum BinaryOperator
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

public static class BinaryOperatorExtensions
{
    public static string Symbol(this BinaryOperator op) => op switch
    {
        BinaryOperator.Add => "+",
        BinaryOperator.Subtract => "-",
        BinaryOperator.Multiply => "*",
        BinaryOperator.Divide => "/",
        BinaryOperator.Modulo => "%",
        BinaryOperator.Equal => "==",
        BinaryOperator.NotEqual => "!=",
        BinaryOperator.Less => "<",
        BinaryOperator.Greater => ">",
        BinaryOperator.LessEqual => "<=",
        BinaryOperator.GreaterEqual => ">=",
        BinaryOperator.LogicalAnd => "&&",
        BinaryOperator.LogicalOr => "||",
        BinaryOperator.BitwiseAnd => "&",
        BinaryOperator.BitwiseOr => "|",
        BinaryOperator.BitwiseXor => "^",
        BinaryOperator.ShiftLeft => "<<",
        BinaryOperator.ShiftRight => ">>",
        _ => throw new ArgumentOutOfRangeException()
    };
}

/// <summary>
/// Represents a unary expression (-a, !a, ~a, &amp;a, *a, ^a, ^!a).
/// </summary>
public record UnaryExpressionNode(UnaryOperator Operator, ExpressionNode Operand) : ExpressionNode
{
    public override string ToString() => $"({Operator.Symbol()}{Operand})";
}

public enum UnaryOperator
{
    Negate,         // -
    LogicalNot,     // !
    BitwiseNot,     // ~
    AddressOf,      // &
    Dereference,    // *
    Ref,            // ^
    MutRef,         // ^!
}

public static class UnaryOperatorExtensions
{
    public static string Symbol(this UnaryOperator op) => op switch
    {
        UnaryOperator.Negate => "-",
        UnaryOperator.LogicalNot => "!",
        UnaryOperator.BitwiseNot => "~",
        UnaryOperator.AddressOf => "&",
        UnaryOperator.Dereference => "*",
        UnaryOperator.Ref => "^",
        UnaryOperator.MutRef => "^!",
        _ => throw new ArgumentOutOfRangeException()
    };
}

/// <summary>
/// Represents a ternary conditional expression (cond ? then : else).
/// </summary>
public record ConditionalExpressionNode(ExpressionNode Condition, ExpressionNode ThenExpr, ExpressionNode ElseExpr) : ExpressionNode
{
    public override string ToString() => $"({Condition} ? {ThenExpr} : {ElseExpr})";
}

/// <summary>
/// Represents a field access expression (obj.field).
/// </summary>
public record FieldAccessExpressionNode(ExpressionNode Target, string FieldName) : ExpressionNode
{
    public override string ToString() => $"{Target}.{FieldName}";
}

/// <summary>
/// Represents a pointer member access expression (ptr-&gt;field).
/// </summary>
public record PointerMemberAccessExpressionNode(ExpressionNode Target, string MemberName) : ExpressionNode
{
    public override string ToString() => $"{Target}->{MemberName}";
}

/// <summary>
/// Represents an index expression (arr[index]).
/// </summary>
public record IndexExpressionNode(ExpressionNode Target, ExpressionNode Index) : ExpressionNode
{
    public override string ToString() => $"{Target}[{Index}]";
}

/// <summary>
/// Represents a function call expression (func(args)).
/// </summary>
public record CallExpressionNode(ExpressionNode Target, IReadOnlyList<CallArgumentNode> Arguments) : ExpressionNode
{
    public override string ToString()
    {
        var args = string.Join(", ", Arguments);
        return $"{Target}({args})";
    }
}

/// <summary>
/// Represents an argument in a function call.
/// </summary>
public record CallArgumentNode(ExpressionNode Value, string? NamedParameter = null)
{
    public override string ToString() => NamedParameter != null ? $"{NamedParameter} = {Value}" : Value.ToString() ?? "";
}

/// <summary>
/// Represents a scoped identifier (Type::member or Type::member(args)).
/// </summary>
public record ScopedAccessExpressionNode(ExpressionNode Target, string MemberName, IReadOnlyList<CallArgumentNode>? ConstructorArgs = null) : ExpressionNode
{
    public override string ToString()
    {
        if (ConstructorArgs != null)
        {
            var args = string.Join(", ", ConstructorArgs);
            return $"{Target}::{MemberName}({args})";
        }
        return $"{Target}::{MemberName}";
    }
}

/// <summary>
/// Represents a simple identifier expression.
/// </summary>
public record IdentifierExpressionNode(string Name) : ExpressionNode
{
    public override string ToString() => Name;
}

/// <summary>
/// Represents a parenthesized expression.
/// </summary>
public record ParenthesizedExpressionNode(ExpressionNode Expression) : ExpressionNode
{
    public override string ToString() => $"({Expression})";
}

/// <summary>
/// Represents a meta expression (@name(args)).
/// </summary>
public record MetaExpressionNode(string Name, IReadOnlyList<MetaArgumentNode>? Arguments = null) : ExpressionNode
{
    public override string ToString()
    {
        if (Arguments != null)
        {
            var args = string.Join(", ", Arguments);
            return $"@{Name}({args})";
        }
        return $"@{Name}";
    }
}

/// <summary>
/// Represents an argument in a meta expression.
/// </summary>
public record MetaArgumentNode
{
    public TypeNode? Type { get; init; }
    public ExpressionNode? Expression { get; init; }
    public string? Identifier { get; init; }

    public override string ToString()
    {
        if (Type != null) return Type.ToString()!;
        if (Expression != null) return Expression.ToString()!;
        if (Identifier != null) return Identifier;
        return "";
    }
}
