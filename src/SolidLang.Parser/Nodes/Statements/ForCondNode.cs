using SolidLang.Parser.Nodes.Expressions;

namespace SolidLang.Parser.Nodes.Statements;

/// <summary>
/// Represents a conditional for loop (while-style): for cond { ... }
/// </summary>
public sealed class ForCondNode : ForKindNode
{
    public ExprNode Condition { get; }
    public BodyStmtNode Body { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ForCondNode(ExprNode condition, BodyStmtNode body, TextSpan span, string fullText)
    {
        Condition = condition;
        Body = body;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ForCondNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Condition;
        yield return Body;
    }

    public override string GetFullText() => _fullText;
}
