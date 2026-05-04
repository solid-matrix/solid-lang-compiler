namespace SolidLangCompiler.SemanticTree;

/// <summary>
/// Base class for all semantic statements.
/// </summary>
public abstract record SemaStatement : SemaNode;

/// <summary>
/// Represents a block of statements.
/// </summary>
public record SemaBlock : SemaStatement
{
    public required IReadOnlyList<SemaStatement> Statements { get; init; }
    public override string ToString()
    {
        var stmts = string.Join("\n  ", Statements);
        return $"{{\n  {stmts}\n}}";
    }
}

/// <summary>
/// Represents a local variable declaration.
/// </summary>
public record SemaLocalDecl : SemaStatement
{
    public required string Name { get; init; }
    public required int Index { get; init; } // Index in local variable table
    public required SemaType Type { get; init; }
    public required SemaExpression? Initializer { get; init; }
    public required bool IsMutable { get; init; }
    public override string ToString()
    {
        var init = Initializer != null ? $" = {Initializer}" : "";
        return $"var {Name}: {Type}{init};";
    }
}

/// <summary>
/// Represents an assignment statement.
/// </summary>
public record SemaAssignment : SemaStatement
{
    public required SemaExpression Target { get; init; }
    public required SemaAssignOp Operator { get; init; }
    public required SemaExpression Value { get; init; }
    public override string ToString() => $"{Target} {Operator.Symbol()} {Value};";
}

/// <summary>
/// Assignment operators.
/// </summary>
public enum SemaAssignOp
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
    ShlAssign,      // <<=
    ShrAssign       // >>=
}

public static class SemaAssignOpExtensions
{
    public static string Symbol(this SemaAssignOp op) => op switch
    {
        SemaAssignOp.Assign => "=",
        SemaAssignOp.AddAssign => "+=",
        SemaAssignOp.SubtractAssign => "-=",
        SemaAssignOp.MultiplyAssign => "*=",
        SemaAssignOp.DivideAssign => "/=",
        SemaAssignOp.ModuloAssign => "%=",
        SemaAssignOp.AndAssign => "&=",
        SemaAssignOp.OrAssign => "|=",
        SemaAssignOp.XorAssign => "^=",
        SemaAssignOp.ShlAssign => "<<=",
        SemaAssignOp.ShrAssign => ">>=",
        _ => throw new ArgumentOutOfRangeException()
    };
}

/// <summary>
/// Represents an expression statement.
/// </summary>
public record SemaExprStmt : SemaStatement
{
    public required SemaExpression Expression { get; init; }
    public override string ToString() => $"{Expression};";
}

/// <summary>
/// Represents a return statement.
/// </summary>
public record SemaReturn : SemaStatement
{
    public required SemaExpression? Value { get; init; }
    public override string ToString() => Value != null ? $"return {Value};" : "return;";
}

/// <summary>
/// Represents an if statement.
/// </summary>
public record SemaIf : SemaStatement
{
    public required SemaExpression Condition { get; init; }
    public required SemaBlock ThenBlock { get; init; }
    public required SemaStatement? ElseBranch { get; init; }
    public override string ToString()
    {
        var elseStr = ElseBranch != null ? $" else {ElseBranch}" : "";
        return $"if {Condition} {ThenBlock}{elseStr}";
    }
}

/// <summary>
/// Represents a while statement.
/// </summary>
public record SemaWhile : SemaStatement
{
    public required SemaExpression Condition { get; init; }
    public required SemaBlock Body { get; init; }
    public override string ToString() => $"while {Condition} {Body}";
}

/// <summary>
/// Represents a for statement (infinite loop).
/// </summary>
public record SemaForInfinite : SemaStatement
{
    public required SemaBlock Body { get; init; }
    public override string ToString() => $"for {Body}";
}

/// <summary>
/// Represents a conditional for loop (while-style).
/// </summary>
public record SemaForConditional : SemaStatement
{
    public required SemaExpression Condition { get; init; }
    public required SemaBlock Body { get; init; }
    public override string ToString() => $"for {Condition} {Body}";
}

/// <summary>
/// Represents a C-style for loop.
/// </summary>
public record SemaForCStyle : SemaStatement
{
    public required SemaStatement? Init { get; init; }
    public required SemaExpression? Condition { get; init; }
    public required SemaExpression? Update { get; init; }
    public required SemaBlock Body { get; init; }
    public override string ToString()
    {
        var init = Init?.ToString() ?? "";
        var cond = Condition?.ToString() ?? "";
        var update = Update?.ToString() ?? "";
        return $"for {init}; {cond}; {update} {Body}";
    }
}

/// <summary>
/// Represents a foreach loop.
/// </summary>
public record SemaForeach : SemaStatement
{
    public required string VariableName { get; init; }
    public required SemaExpression Iterable { get; init; }
    public required SemaBlock Body { get; init; }
    public required int VariableIndex { get; init; }
    public required SemaType VariableType { get; init; }
    public override string ToString() => $"for var {VariableName} in {Iterable} {Body}";
}

/// <summary>
/// Represents a break statement.
/// </summary>
public record SemaBreak : SemaStatement
{
    public override string ToString() => "break;";
}

/// <summary>
/// Represents a continue statement.
/// </summary>
public record SemaContinue : SemaStatement
{
    public override string ToString() => "continue;";
}

/// <summary>
/// Represents an empty statement.
/// </summary>
public record SemaEmpty : SemaStatement
{
    public override string ToString() => ";";
}

/// <summary>
/// Represents a switch statement.
/// </summary>
public record SemaSwitch : SemaStatement
{
    public required SemaExpression Expression { get; init; }
    public required IReadOnlyList<SemaSwitchArm> Arms { get; init; }
    public override string ToString()
    {
        var arms = string.Join("\n  ", Arms);
        return $"switch {Expression} {{\n  {arms}\n}}";
    }
}

/// <summary>
/// Represents a switch arm.
/// </summary>
public record SemaSwitchArm : SemaNode
{
    public required SemaPattern? Pattern { get; init; }
    public required SemaStatement Statement { get; init; }
    public override string ToString()
    {
        var pattern = Pattern?.ToString() ?? "else";
        return $"{pattern} => {Statement}";
    }
}

/// <summary>
/// Base class for patterns.
/// </summary>
public abstract record SemaPattern : SemaNode;

/// <summary>
/// Represents a literal pattern.
/// </summary>
public record SemaLiteralPattern : SemaPattern
{
    public required SemaExpression Literal { get; init; }
    public override string ToString() => Literal.ToString()!;
}

/// <summary>
/// Represents an enum/variant pattern (Type::Member or Type::Member(binding)).
/// </summary>
public record SemaTypePattern : SemaPattern
{
    public required string TypeName { get; init; }
    public required string MemberName { get; init; }
    public IReadOnlyList<SemaPattern>? Bindings { get; init; }
    public override string ToString()
    {
        if (Bindings != null)
        {
            var bindings = string.Join(", ", Bindings);
            return $"{TypeName}::{MemberName}({bindings})";
        }
        return $"{TypeName}::{MemberName}";
    }
}

/// <summary>
/// Represents an identifier pattern (variable binding).
/// </summary>
public record SemaIdentifierPattern : SemaPattern
{
    public required string Name { get; init; }
    public override string ToString() => Name;
}
