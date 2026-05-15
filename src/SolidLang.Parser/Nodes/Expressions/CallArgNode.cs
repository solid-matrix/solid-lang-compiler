namespace SolidLang.Parser.Nodes.Expressions;

/// <summary>
/// Represents a call argument.
/// </summary>
public sealed class CallArgNode : SyntaxNode
{
    public ExprNode Expression { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CallArgNode(ExprNode expression, TextSpan span, string fullText)
    {
        Expression = expression;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CallArgNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
    }

    public override string GetFullText() => _fullText;
}
