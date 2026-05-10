using SolidLang.Parser;

namespace SolidLang.SemanticAnalyzer;

// ========================================
// Bound Statement nodes
// ========================================

/// <summary>
/// Abstract base for all bound statements.
/// </summary>
public abstract class BoundStatement : BoundNode { }

/// <summary>
/// A block of statements: { stmt* }
/// </summary>
public sealed class BoundBlock : BoundStatement
{
    public override BoundKind Kind => BoundKind.Block;
    public Scope Scope { get; }
    public IReadOnlyList<BoundStatement> Statements { get; }

    public BoundBlock(Scope scope, IReadOnlyList<BoundStatement> statements)
    {
        Scope = scope;
        Statements = statements;
    }
}

/// <summary>
/// A variable declaration used as a statement (var/const/static in a block).
/// </summary>
public sealed class BoundVariableStmt : BoundStatement
{
    public override BoundKind Kind => BoundKind.VariableStmt;
    public BoundVariableDecl Declaration { get; }

    public BoundVariableStmt(BoundVariableDecl declaration) { Declaration = declaration; }
}

/// <summary>
/// An expression used as a statement (e.g., a bare function call).
/// </summary>
public sealed class BoundExprStmt : BoundStatement
{
    public override BoundKind Kind => BoundKind.ExprStmt;
    public BoundExpression Expression { get; }

    public BoundExprStmt(BoundExpression expression) { Expression = expression; }
}

/// <summary>
/// An assignment statement: target op= value.
/// </summary>
public sealed class BoundAssignStmt : BoundStatement
{
    public override BoundKind Kind => BoundKind.AssignStmt;
    public BoundExpression Target { get; }
    public SyntaxKind Operator { get; }
    public BoundExpression Value { get; }

    public BoundAssignStmt(BoundExpression target, SyntaxKind op, BoundExpression value)
    {
        Target = target;
        Operator = op;
        Value = value;
    }
}

/// <summary>
/// An if statement: if cond { then } else { else }.
/// </summary>
public sealed class BoundIfStmt : BoundStatement
{
    public override BoundKind Kind => BoundKind.IfStmt;
    public BoundExpression Condition { get; }
    public BoundBlock ThenBody { get; }
    public BoundStatement? ElseBody { get; }  // BoundBlock or BoundIfStmt (for else-if)

    public BoundIfStmt(BoundExpression condition, BoundBlock thenBody, BoundStatement? elseBody)
    {
        Condition = condition;
        ThenBody = thenBody;
        ElseBody = elseBody;
    }
}

/// <summary>
/// A while statement: while cond { body } or while { body } (infinite).
/// When Condition is null, this is an infinite loop.
/// </summary>
public sealed class BoundWhileStmt : BoundStatement
{
    public override BoundKind Kind => BoundKind.WhileStmt;
    public BoundExpression? Condition { get; }  // null = infinite
    public BoundBlock Body { get; }

    public BoundWhileStmt(BoundExpression? condition, BoundBlock body)
    {
        Condition = condition;
        Body = body;
    }
}

/// <summary>
/// A C-style for statement: for (init; cond; update) { body }.
/// Only one of InitVariable or InitExpression is non-null.
/// </summary>
public sealed class BoundForCStyleStmt : BoundStatement
{
    public override BoundKind Kind => BoundKind.ForCStyleStmt;
    public BoundVariableDecl? InitVariable { get; }    // for var i = 0; ...
    public BoundExpression? InitExpression { get; }    // for i = 0; ... (bare assignment expr)
    public BoundExpression? Condition { get; }
    public BoundExpression? Update { get; }
    public BoundBlock Body { get; }

    public BoundForCStyleStmt(BoundVariableDecl? initVariable, BoundExpression? initExpression,
        BoundExpression? condition, BoundExpression? update, BoundBlock body)
    {
        InitVariable = initVariable;
        InitExpression = initExpression;
        Condition = condition;
        Update = update;
        Body = body;
    }
}

/// <summary>
/// A switch statement: switch expr { arm* }.
/// </summary>
public sealed class BoundSwitchStmt : BoundStatement
{
    public override BoundKind Kind => BoundKind.SwitchStmt;
    public BoundExpression Expression { get; }
    public IReadOnlyList<BoundSwitchArm> Arms { get; }

    public BoundSwitchStmt(BoundExpression expression, IReadOnlyList<BoundSwitchArm> arms)
    {
        Expression = expression;
        Arms = arms;
    }
}

/// <summary>
/// A single switch arm: pattern => stmt.
/// </summary>
public sealed class BoundSwitchArm : BoundNode
{
    public override BoundKind Kind => BoundKind.SwitchArm;
    public bool IsElse { get; }
    public IReadOnlyList<BoundSwitchPattern> Patterns { get; }
    public BoundStatement Body { get; }
    public Scope ArmScope { get; }  // For pattern-capture variable bindings

    public BoundSwitchArm(bool isElse, IReadOnlyList<BoundSwitchPattern> patterns,
        BoundStatement body, Scope armScope)
    {
        IsElse = isElse;
        Patterns = patterns;
        Body = body;
        ArmScope = armScope;
    }
}

/// <summary>
/// A return statement: return expr?;
/// </summary>
public sealed class BoundReturnStmt : BoundStatement
{
    public override BoundKind Kind => BoundKind.ReturnStmt;
    public BoundExpression? Expression { get; }

    public BoundReturnStmt(BoundExpression? expression) { Expression = expression; }
}

/// <summary>
/// A break statement: break;
/// </summary>
public sealed class BoundBreakStmt : BoundStatement
{
    public override BoundKind Kind => BoundKind.BreakStmt;
}

/// <summary>
/// A continue statement: continue;
/// </summary>
public sealed class BoundContinueStmt : BoundStatement
{
    public override BoundKind Kind => BoundKind.ContinueStmt;
}

/// <summary>
/// A defer statement: defer stmt;
/// </summary>
public sealed class BoundDeferStmt : BoundStatement
{
    public override BoundKind Kind => BoundKind.DeferStmt;
    public BoundStatement DeferredStatement { get; }

    public BoundDeferStmt(BoundStatement deferredStatement) { DeferredStatement = deferredStatement; }
}
