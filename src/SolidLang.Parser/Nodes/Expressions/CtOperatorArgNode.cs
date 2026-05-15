using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Expressions;

/// <summary>
/// Represents a compile-time operator argument (type or expression).
/// </summary>
public sealed class CtOperatorArgNode : SyntaxNode
{
    public TypeNode? Type { get; }
    public ExprNode? Expression { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CtOperatorArgNode(TypeNode? type, ExprNode? expression, TextSpan span, string fullText)
    {
        Type = type;
        Expression = expression;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CtOperatorArgNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Type != null)
            yield return Type;
        if (Expression != null)
            yield return Expression;
    }

    public override string GetFullText() => _fullText;
}
