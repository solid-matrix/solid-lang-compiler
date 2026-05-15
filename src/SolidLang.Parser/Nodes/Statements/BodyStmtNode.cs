namespace SolidLang.Parser.Nodes.Statements;

/// <summary>
/// Represents a body statement (block): { stmt* }
/// </summary>
public sealed class BodyStmtNode : StmtNode
{
    public IReadOnlyList<StmtNode> Statements { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public BodyStmtNode(IReadOnlyList<StmtNode> statements, TextSpan span, string fullText)
    {
        Statements = statements;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.BodyStmtNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Statements;

    public override string GetFullText() => _fullText;
}
