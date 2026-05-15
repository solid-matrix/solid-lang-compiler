namespace SolidLang.Parser.Nodes.Expressions;

/// <summary>
/// Represents a conditional (ternary) expression: cond ? then : else
/// </summary>
public sealed class ConditionalExprNode : ExprNode
{
    public ExprNode Condition { get; }
    public ExprNode ThenExpr { get; }
    public ExprNode ElseExpr { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ConditionalExprNode(ExprNode condition, ExprNode thenExpr, ExprNode elseExpr, TextSpan span, string fullText)
    {
        Condition = condition;
        ThenExpr = thenExpr;
        ElseExpr = elseExpr;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ConditionalExprNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Condition;
        yield return ThenExpr;
        yield return ElseExpr;
    }

    public override string GetFullText() => _fullText;
}
