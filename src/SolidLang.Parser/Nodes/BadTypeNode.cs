namespace SolidLang.Parser.Nodes;

/// <summary>
/// Represents a bad type (used for error recovery).
/// </summary>
public sealed class BadTypeNode : Types.TypeNode
{
    private readonly TextSpan _span;
    private readonly string _fullText;

    public BadTypeNode(TextSpan span = default, string fullText = "")
    {
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.BadTypeNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => _fullText;
}
