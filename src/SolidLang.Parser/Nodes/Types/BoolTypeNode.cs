namespace SolidLang.Parser.Nodes.Types;

/// <summary>
/// Represents the bool type.
/// </summary>
public sealed class BoolTypeNode : TypeNode
{
    private readonly TextSpan _span;

    public BoolTypeNode(TextSpan span)
    {
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.BoolTypeNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => "bool";
}
