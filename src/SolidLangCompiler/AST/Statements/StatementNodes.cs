using SolidLangCompiler.AST.Expressions;
using SolidLangCompiler.AST.Types;

namespace SolidLangCompiler.AST.Statements;

/// <summary>
/// Base class for all statement nodes.
/// </summary>
public abstract record StatementNode : AstNode;

/// <summary>
/// Represents an empty statement (just a semicolon).
/// </summary>
public record EmptyStatementNode() : StatementNode
{
    public override string ToString() => ";";
}

/// <summary>
/// Represents a local variable declaration statement.
/// </summary>
public record VarDeclStatementNode(
    IReadOnlyList<Declarations.AnnotationNode>? Annotations,
    string Name,
    Types.TypeNode? Type,
    Expressions.ExpressionNode? Initializer
) : StatementNode
{
    public override string ToString()
    {
        var type = Type != null ? $": {Type}" : "";
        var init = Initializer != null ? $" = {Initializer}" : "";
        return $"var {Name}{type}{init};";
    }
}

/// <summary>
/// Represents a block statement { stmts }.
/// </summary>
public record BlockStatementNode(IReadOnlyList<StatementNode> Statements) : StatementNode
{
    public override string ToString()
    {
        var stmts = string.Join("\n  ", Statements);
        return $"{{\n  {stmts}\n}}";
    }
}

/// <summary>
/// Represents an assignment statement (a = b; or a += b; etc.).
/// </summary>
public record AssignmentStatementNode(ExpressionNode Target, AssignmentOperator Operator, ExpressionNode Value) : StatementNode
{
    public override string ToString() => $"{Target} {Operator.Symbol()} {Value};";
}

public enum AssignmentOperator
{
    Assign,         // =
    AddAssign,      // +=
    SubtractAssign, // -=
    MultiplyAssign, // *=
    DivideAssign,   // /=
    ModuloAssign,   // %=
    AndAssign,      // &=
    OrAssign,       // |=
    XorAssign,      // ^=
    ShiftLeftAssign,  // <<=
    ShiftRightAssign, // >>=
}

public static class AssignmentOperatorExtensions
{
    public static string Symbol(this AssignmentOperator op) => op switch
    {
        AssignmentOperator.Assign => "=",
        AssignmentOperator.AddAssign => "+=",
        AssignmentOperator.SubtractAssign => "-=",
        AssignmentOperator.MultiplyAssign => "*=",
        AssignmentOperator.DivideAssign => "/=",
        AssignmentOperator.ModuloAssign => "%=",
        AssignmentOperator.AndAssign => "&=",
        AssignmentOperator.OrAssign => "|=",
        AssignmentOperator.XorAssign => "^=",
        AssignmentOperator.ShiftLeftAssign => "<<=",
        AssignmentOperator.ShiftRightAssign => ">>=",
        _ => throw new ArgumentOutOfRangeException()
    };
}

/// <summary>
/// Represents an expression statement (expr;).
/// </summary>
public record ExpressionStatementNode(ExpressionNode Expression) : StatementNode
{
    public override string ToString() => $"{Expression};";
}

/// <summary>
/// Represents a defer statement (defer stmt).
/// Deferred statements execute when leaving the current scope in LIFO order.
/// </summary>
public record DeferStatementNode(StatementNode DeferredStatement) : StatementNode
{
    public override string ToString() => $"defer {DeferredStatement}";
}

/// <summary>
/// Represents an if statement.
/// </summary>
public record IfStatementNode(ExpressionNode Condition, BlockStatementNode ThenBlock, StatementNode? ElseBranch = null) : StatementNode
{
    public override string ToString()
    {
        var elseStr = ElseBranch != null ? $" else {ElseBranch}" : "";
        return $"if {Condition} {ThenBlock}{elseStr}";
    }
}

/// <summary>
/// Represents a for statement (infinite, conditional, C-style, or foreach).
/// </summary>
public record ForStatementNode(ForKind Kind) : StatementNode;

/// <summary>
/// Represents an infinite for loop.
/// </summary>
public record InfiniteForNode(BlockStatementNode Body) : ForStatementNode(ForKind.Infinite)
{
    public override string ToString() => $"for {Body}";
}

/// <summary>
/// Represents a conditional for loop (while-style).
/// </summary>
public record ConditionalForNode(ExpressionNode Condition, BlockStatementNode Body) : ForStatementNode(ForKind.Conditional)
{
    public override string ToString() => $"for {Condition} {Body}";
}

/// <summary>
/// Represents a C-style for loop.
/// </summary>
public record CStyleForNode(StatementNode? Init, ExpressionNode? Condition, ExpressionNode? Update, BlockStatementNode Body) : ForStatementNode(ForKind.CStyle)
{
    public override string ToString()
    {
        var init = Init?.ToString() ?? ";";
        var cond = Condition?.ToString() ?? "";
        var update = Update?.ToString() ?? "";
        return $"for {init} {cond}; {update} {Body}";
    }
}

/// <summary>
/// Represents a foreach loop.
/// </summary>
public record ForeachNode(string VariableName, ExpressionNode Iterable, BlockStatementNode Body) : ForStatementNode(ForKind.Foreach)
{
    public override string ToString() => $"for var {VariableName} in {Iterable} {Body}";
}

public enum ForKind
{
    Infinite,
    Conditional,
    CStyle,
    Foreach
}

/// <summary>
/// Represents a switch statement.
/// </summary>
public record SwitchStatementNode(ExpressionNode Expression, IReadOnlyList<SwitchArmNode> Arms) : StatementNode
{
    public override string ToString()
    {
        var arms = string.Join("\n  ", Arms);
        return $"switch {Expression} {{\n  {arms}\n}}";
    }
}

/// <summary>
/// Represents a switch arm.
/// </summary>
public record SwitchArmNode(PatternNode? Pattern, StatementNode Statement) : AstNode
{
    public override string ToString()
    {
        var pattern = Pattern?.ToString() ?? "else";
        return $"{pattern} => {Statement}";
    }
}

/// <summary>
/// Base class for patterns.
/// </summary>
public abstract record PatternNode : AstNode;

/// <summary>
/// Represents a literal pattern.
/// </summary>
public record LiteralPatternNode(LiteralExpressionNode Literal) : PatternNode
{
    public override string ToString() => Literal.ToString()!;
}

/// <summary>
/// Represents an enum/variant pattern (Type::Member or Type::Member(binding)).
/// </summary>
public record TypePatternNode(NamedTypeNode Type, string MemberName, IReadOnlyList<PatternNode>? Bindings = null) : PatternNode
{
    public override string ToString()
    {
        if (Bindings != null)
        {
            var bindings = string.Join(", ", Bindings);
            return $"{Type}::{MemberName}({bindings})";
        }
        return $"{Type}::{MemberName}";
    }
}

/// <summary>
/// Represents an identifier pattern (variable binding).
/// </summary>
public record IdentifierPatternNode(string Name) : PatternNode
{
    public override string ToString() => Name;
}

/// <summary>
/// Represents a break statement.
/// </summary>
public record BreakStatementNode() : StatementNode
{
    public override string ToString() => "break;";
}

/// <summary>
/// Represents a continue statement.
/// </summary>
public record ContinueStatementNode() : StatementNode
{
    public override string ToString() => "continue;";
}

/// <summary>
/// Represents a return statement.
/// </summary>
public record ReturnStatementNode(ExpressionNode? Value = null) : StatementNode
{
    public override string ToString() => Value != null ? $"return {Value};" : "return;";
}
