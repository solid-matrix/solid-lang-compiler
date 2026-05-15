namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents a namespace path: a::b::c
/// </summary>
public sealed class NamespacePathNode : SyntaxNode
{
    public IReadOnlyList<string> Segments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public NamespacePathNode(IReadOnlyList<string> segments, TextSpan span, string fullText)
    {
        Segments = segments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.NamespacePathNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{string.Join("::", Segments)}]");
    }
}
