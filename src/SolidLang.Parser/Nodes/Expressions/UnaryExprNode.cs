namespace SolidLang.Parser.Nodes.Expressions;

/// <summary>
/// Represents a unary expression: op expr
/// </summary>
public sealed class UnaryExprNode : ExprNode
{
    public SyntaxKind Operator { get; }
    public ExprNode Operand { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public UnaryExprNode(SyntaxKind op, ExprNode operand, TextSpan span, string fullText)
    {
        Operator = op;
        Operand = operand;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.UnaryExprNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Operand;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Operator}]");
    }
}
