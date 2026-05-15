namespace SolidLang.Parser.Nodes.Expressions;

/// <summary>
/// Represents compile-time operator arguments.
/// </summary>
public sealed class CtOperatorArgsNode : SyntaxNode
{
    public IReadOnlyList<CtOperatorArgNode> Arguments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CtOperatorArgsNode(IReadOnlyList<CtOperatorArgNode> arguments, TextSpan span, string fullText)
    {
        Arguments = arguments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CtOperatorArgsNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Arguments;

    public override string GetFullText() => _fullText;
}
