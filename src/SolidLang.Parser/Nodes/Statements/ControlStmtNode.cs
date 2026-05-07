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

/// <summary>
/// Represents an if statement: if expr body else (body | if)
/// </summary>
public sealed class IfStmtNode : StmtNode
{
    public Expressions.ExprNode Condition { get; }
    public BodyStmtNode ThenBody { get; }
    public StmtNode? ElseBody { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public IfStmtNode(Expressions.ExprNode condition, BodyStmtNode thenBody, StmtNode? elseBody, TextSpan span, string fullText)
    {
        Condition = condition;
        ThenBody = thenBody;
        ElseBody = elseBody;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.IfStmtNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Condition;
        yield return ThenBody;
        if (ElseBody != null)
            yield return ElseBody;
    }

    public override string GetFullText() => _fullText;
}
