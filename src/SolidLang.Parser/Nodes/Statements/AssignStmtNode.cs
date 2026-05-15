using SolidLang.Parser.Nodes.Expressions;

namespace SolidLang.Parser.Nodes.Statements;

/// <summary>
/// Represents an assignment statement: expr = expr;
/// </summary>
public sealed class AssignStmtNode : StmtNode
{
    public ExprNode Target { get; }
    public SyntaxKind Operator { get; }
    public ExprNode Value { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public AssignStmtNode(ExprNode target, SyntaxKind op, ExprNode value, TextSpan span, string fullText)
    {
        Target = target;
        Operator = op;
        Value = value;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.AssignStmtNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Target;
        yield return Value;
    }

    public override string GetFullText() => _fullText;
}
