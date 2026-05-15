namespace SolidLang.Parser.Nodes.Statements;

/// <summary>
/// Represents a defer statement: defer stmt
/// </summary>
public sealed class DeferStmtNode : StmtNode
{
    public StmtNode Statement { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public DeferStmtNode(StmtNode statement, TextSpan span, string fullText)
    {
        Statement = statement;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.DeferStmtNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Statement;
    }

    public override string GetFullText() => _fullText;
}
