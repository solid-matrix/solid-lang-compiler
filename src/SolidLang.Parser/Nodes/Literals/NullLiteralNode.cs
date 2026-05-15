namespace SolidLang.Parser.Nodes.Literals;

/// <summary>
/// Represents a null literal.
/// </summary>
public sealed class NullLiteralNode : LiteralNode
{
    private readonly TextSpan _span;

    public NullLiteralNode(TextSpan span)
    {
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.NullLiteralNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => "null";
}
