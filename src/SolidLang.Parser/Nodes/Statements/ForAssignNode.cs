using SolidLang.Parser.Nodes.Expressions;

namespace SolidLang.Parser.Nodes.Statements;

/// <summary>
/// Represents a for loop assignment: expr = expr
/// </summary>
public sealed class ForAssignNode : ForInitNode
{
    public ExprNode Target { get; }
    public SyntaxKind Operator { get; }
    public ExprNode Value { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ForAssignNode(ExprNode target, SyntaxKind op, ExprNode value, TextSpan span, string fullText)
    {
        Target = target;
        Operator = op;
        Value = value;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ForAssignNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Target;
        yield return Value;
    }

    public override string GetFullText() => _fullText;
}
