using SolidLang.Parser.Nodes.Expressions;

namespace SolidLang.Parser.Nodes;

/// <summary>
/// Represents a bad expression (used for error recovery).
/// </summary>
public sealed class BadExprNode : ExprNode
{
    private readonly TextSpan _span;
    private readonly string _fullText;

    public BadExprNode(TextSpan span = default, string fullText = "")
    {
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.BadExprNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => _fullText;
}
