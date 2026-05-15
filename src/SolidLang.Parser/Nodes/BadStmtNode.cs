namespace SolidLang.Parser.Nodes;

/// <summary>
/// Represents a bad statement (used for error recovery).
/// </summary>
public sealed class BadStmtNode : Statements.StmtNode
{
    private readonly TextSpan _span;
    private readonly string _fullText;

    public BadStmtNode(TextSpan span = default, string fullText = "")
    {
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.BadStmtNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => _fullText;
}
