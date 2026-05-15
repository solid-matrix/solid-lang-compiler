namespace SolidLang.Parser.Nodes.Statements;

/// <summary>
/// Represents break statement: break;
/// </summary>
public sealed class BreakStmtNode : StmtNode
{
    private readonly TextSpan _span;

    public BreakStmtNode(TextSpan span)
    {
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.BreakStmtNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => "break;";
}
