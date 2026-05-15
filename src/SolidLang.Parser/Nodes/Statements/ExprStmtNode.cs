using SolidLang.Parser.Nodes.Expressions;

namespace SolidLang.Parser.Nodes.Statements;

/// <summary>
/// Represents an expression statement: expr;
/// </summary>
public sealed class ExprStmtNode : StmtNode
{
    public ExprNode Expression { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ExprStmtNode(ExprNode expression, TextSpan span, string fullText)
    {
        Expression = expression;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ExprStmtNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
    }

    public override string GetFullText() => _fullText;
}
