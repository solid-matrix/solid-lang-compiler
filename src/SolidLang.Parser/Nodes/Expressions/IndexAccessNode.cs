namespace SolidLang.Parser.Nodes.Expressions;

/// <summary>
/// Represents an index access: [expr]
/// </summary>
public sealed class IndexAccessNode : PostfixSuffixNode
{
    public ExprNode Index { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public IndexAccessNode(ExprNode index, TextSpan span, string fullText)
    {
        Index = index;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.IndexAccessNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Index;
    }

    public override string GetFullText() => _fullText;
}
