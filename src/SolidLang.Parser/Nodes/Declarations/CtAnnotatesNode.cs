namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents compile-time annotations: @attr1 @attr2(args)
/// </summary>
public sealed class CtAnnotatesNode : SyntaxNode
{
    public IReadOnlyList<CtAnnotateNode> Annotations { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CtAnnotatesNode(IReadOnlyList<CtAnnotateNode> annotations, TextSpan span, string fullText)
    {
        Annotations = annotations;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CtAnnotatesNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Annotations;

    public override string GetFullText() => _fullText;
}
