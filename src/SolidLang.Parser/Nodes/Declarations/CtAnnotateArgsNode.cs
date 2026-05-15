namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents annotation arguments: arg1, arg2, ...
/// </summary>
public sealed class CtAnnotateArgsNode : SyntaxNode
{
    public IReadOnlyList<CtAnnotateArgNode> Arguments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CtAnnotateArgsNode(IReadOnlyList<CtAnnotateArgNode> arguments, TextSpan span, string fullText)
    {
        Arguments = arguments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CtAnnotateArgsNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Arguments;

    public override string GetFullText() => _fullText;
}
