using SolidLang.Parser.Nodes.Expressions;

namespace SolidLang.Parser.Nodes.Statements;

/// <summary>
/// Represents an assignment statement: expr = expr;
/// </summary>
public sealed class AssignStmtNode : StmtNode
{
    public ExprNode Target { get; }
    public SyntaxKind Operator { get; }
    public ExprNode Value { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public AssignStmtNode(ExprNode target, SyntaxKind op, ExprNode value, TextSpan span, string fullText)
    {
        Target = target;
        Operator = op;
        Value = value;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.AssignStmtNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Target;
        yield return Value;
    }

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents an expression statement: expr;
/// </summary>
public sealed class ExprStmtNode : StmtNode
{
    public ExprNode Expression { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ExprStmtNode(ExprNode expression, TextSpan span, string fullText)
    {
        Expression = expression;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ExprStmtNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
    }

    public override string GetFullText() => _fullText;
}
