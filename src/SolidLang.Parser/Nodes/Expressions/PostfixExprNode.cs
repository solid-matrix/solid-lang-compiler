namespace SolidLang.Parser.Nodes.Expressions;

/// <summary>
/// Represents a postfix expression: expr.suffix
/// </summary>
public sealed class PostfixExprNode : ExprNode
{
    public ExprNode Primary { get; }
    public IReadOnlyList<PostfixSuffixNode> Suffixes { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public PostfixExprNode(ExprNode primary, IReadOnlyList<PostfixSuffixNode> suffixes, TextSpan span, string fullText)
    {
        Primary = primary;
        Suffixes = suffixes;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.PostfixExprNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Primary;
        foreach (var suffix in Suffixes)
            yield return suffix;
    }

    public override string GetFullText() => _fullText;
}
