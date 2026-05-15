namespace SolidLang.Parser.Nodes.Statements;

/// <summary>
/// Represents continue statement: continue;
/// </summary>
public sealed class ContinueStmtNode : StmtNode
{
    private readonly TextSpan _span;

    public ContinueStmtNode(TextSpan span)
    {
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.ContinueStmtNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => "continue;";
}
