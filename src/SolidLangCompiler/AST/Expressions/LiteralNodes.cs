using SolidLangCompiler.AST.Types;

namespace SolidLangCompiler.AST.Expressions;

/// <summary>
/// Base class for literal expressions.
/// </summary>
public abstract record LiteralExpressionNode : ExpressionNode;

/// <summary>
/// Represents an integer literal.
/// </summary>
public record IntegerLiteralNode(ulong Value, IntegerKind? Suffix = null) : LiteralExpressionNode
{
    public override string ToString()
    {
        var suffix = Suffix.HasValue ? Suffix.Value.ToString().ToLower() : "";
        return $"{Value}{suffix}";
    }
}

/// <summary>
/// Represents a floating-point literal.
/// </summary>
public record FloatLiteralNode(double Value, FloatKind? Suffix = null) : LiteralExpressionNode
{
    public override string ToString()
    {
        var suffix = Suffix.HasValue ? Suffix.Value.ToString().ToLower() : "";
        return $"{Value}{suffix}";
    }
}

/// <summary>
/// Represents a string literal.
/// </summary>
public record StringLiteralNode(string Value) : LiteralExpressionNode
{
    public override string ToString() => $"\"{Value}\"";
}

/// <summary>
/// Represents a character literal.
/// </summary>
public record CharLiteralNode(string Value) : LiteralExpressionNode
{
    public override string ToString() => $"'{Value}'";
}

/// <summary>
/// Represents a boolean literal.
/// </summary>
public record BoolLiteralNode(bool Value) : LiteralExpressionNode
{
    public override string ToString() => Value ? "true" : "false";
}

/// <summary>
/// Represents the null literal.
/// </summary>
public record NullLiteralNode() : LiteralExpressionNode
{
    public override string ToString() => "null";
}

/// <summary>
/// Represents an array literal [size]type{elems}.
/// </summary>
public record ArrayLiteralNode(ArrayTypeNode? ArrayType, IReadOnlyList<ExpressionNode>? Elements) : LiteralExpressionNode
{
    public override string ToString()
    {
        var typeStr = ArrayType?.ToString() ?? "";
        var elemsStr = Elements != null ? string.Join(", ", Elements) : "";
        return $"{typeStr}{{{elemsStr}}}";
    }
}

/// <summary>
/// Represents a struct literal Type{field = value, ...}.
/// </summary>
public record StructLiteralNode(NamedTypeNode Type, IReadOnlyList<StructLiteralFieldNode>? Fields) : LiteralExpressionNode
{
    public override string ToString()
    {
        var fields = Fields != null ? string.Join(", ", Fields) : "";
        return $"{Type}{{{fields}}}";
    }
}

/// <summary>
/// Represents a field in a struct literal.
/// </summary>
public record StructLiteralFieldNode(string Name, ExpressionNode Value)
{
    public override string ToString() => $"{Name} = {Value}";
}

/// <summary>
/// Represents a union literal Type::field(value).
/// </summary>
public record UnionLiteralNode(NamedTypeNode Type, string FieldName, ExpressionNode Value) : LiteralExpressionNode
{
    public override string ToString() => $"{Type}::{FieldName}({Value})";
}

/// <summary>
/// Represents an enum literal Type::Member.
/// </summary>
public record EnumLiteralNode(NamedTypeNode Type, string MemberName) : LiteralExpressionNode
{
    public override string ToString() => $"{Type}::{MemberName}";
}

/// <summary>
/// Represents a variant literal Type::Member(value) or Type::Member.
/// </summary>
public record VariantLiteralNode(NamedTypeNode Type, string MemberName, ExpressionNode? Value = null) : LiteralExpressionNode
{
    public override string ToString() => Value != null ? $"{Type}::{MemberName}({Value})" : $"{Type}::{MemberName}";
}

/// <summary>
/// Represents a tuple literal (expr: type, expr: type, ...).
/// </summary>
public record TupleLiteralNode(IReadOnlyList<TupleLiteralElementNode> Elements) : LiteralExpressionNode
{
    public override string ToString() => $"({string.Join(", ", Elements)})";
}

/// <summary>
/// Represents an element in a tuple literal.
/// </summary>
public record TupleLiteralElementNode(ExpressionNode Value, TypeNode Type)
{
    public override string ToString() => $"{Value}: {Type}";
}
