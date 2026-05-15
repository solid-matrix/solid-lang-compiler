namespace SolidLang.Parser.Nodes.Expressions;

/// <summary>
/// Represents call arguments: arg1, arg2, ...
/// </summary>
public sealed class CallArgsNode : SyntaxNode
{
    public IReadOnlyList<CallArgNode> Arguments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CallArgsNode(IReadOnlyList<CallArgNode> arguments, TextSpan span, string fullText)
    {
        Arguments = arguments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CallArgsNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Arguments;

    public override string GetFullText() => _fullText;
}
