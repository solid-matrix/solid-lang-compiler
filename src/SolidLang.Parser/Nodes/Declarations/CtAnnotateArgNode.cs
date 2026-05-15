using SolidLang.Parser.Nodes.Expressions;
using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents an annotation argument (type or expression).
/// </summary>
public sealed class CtAnnotateArgNode : SyntaxNode
{
    public Types.TypeNode? Type { get; }
    public ExprNode? Expression { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CtAnnotateArgNode(Types.TypeNode? type, ExprNode? expression, TextSpan span, string fullText)
    {
        Type = type;
        Expression = expression;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CtAnnotateArgNode;
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
