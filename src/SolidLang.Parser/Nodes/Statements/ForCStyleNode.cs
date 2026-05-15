using SolidLang.Parser.Nodes.Expressions;

namespace SolidLang.Parser.Nodes.Statements;

/// <summary>
/// Represents a C-style for loop: for init; cond; update { ... }
/// </summary>
public sealed class ForCStyleNode : ForKindNode
{
    public ForInitNode? Init { get; }
    public ExprNode? Condition { get; }
    public ExprNode? Update { get; }
    public BodyStmtNode Body { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ForCStyleNode(ForInitNode? init, ExprNode? condition, ExprNode? update, BodyStmtNode body, TextSpan span, string fullText)
    {
        Init = init;
        Condition = condition;
        Update = update;
        Body = body;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ForCStyleNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Init != null)
            yield return Init;
        if (Condition != null)
            yield return Condition;
        if (Update != null)
            yield return Update;
        yield return Body;
    }

    public override string GetFullText() => _fullText;
}
