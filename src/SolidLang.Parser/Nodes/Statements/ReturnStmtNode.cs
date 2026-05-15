using SolidLang.Parser.Nodes.Expressions;

namespace SolidLang.Parser.Nodes.Statements;

/// <summary>
/// Represents return statement: return expr? ;
/// </summary>
public sealed class ReturnStmtNode : StmtNode
{
    public ExprNode? Expression { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ReturnStmtNode(ExprNode? expression, TextSpan span, string fullText)
    {
        Expression = expression;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ReturnStmtNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Expression != null)
            yield return Expression;
    }

    public override string GetFullText() => _fullText;
}
