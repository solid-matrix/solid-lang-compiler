namespace SolidLang.Parser.Nodes.Expressions;

/// <summary>
/// Represents a call expression: (args)
/// </summary>
public sealed class CallExprNode : PostfixSuffixNode
{
    public IReadOnlyList<CallArgNode> Arguments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CallExprNode(IReadOnlyList<CallArgNode> arguments, TextSpan span, string fullText)
    {
        Arguments = arguments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CallExprNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var a in Arguments)
            yield return a;
    }

    public override string GetFullText() => _fullText;
}
