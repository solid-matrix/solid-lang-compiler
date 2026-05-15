namespace SolidLang.Parser.Nodes.Statements;

/// <summary>
/// Represents an empty statement: ;
/// </summary>
public sealed class EmptyStmtNode : StmtNode
{
    private readonly TextSpan _span;

    public EmptyStmtNode(TextSpan span)
    {
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.EmptyStmtNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => ";";
}
