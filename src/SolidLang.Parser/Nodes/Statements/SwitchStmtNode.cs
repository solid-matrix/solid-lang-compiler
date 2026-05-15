using SolidLang.Parser.Nodes.Expressions;

namespace SolidLang.Parser.Nodes.Statements;

/// <summary>
/// Represents a switch statement: switch expr { arms }
/// </summary>
public sealed class SwitchStmtNode : StmtNode
{
    public ExprNode Expression { get; }
    public IReadOnlyList<SwitchArmNode> Arms { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public SwitchStmtNode(ExprNode expression, IReadOnlyList<SwitchArmNode> arms, TextSpan span, string fullText)
    {
        Expression = expression;
        Arms = arms;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.SwitchStmtNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
        foreach (var arm in Arms)
            yield return arm;
    }

    public override string GetFullText() => _fullText;
}
