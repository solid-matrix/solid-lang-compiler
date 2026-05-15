namespace SolidLang.Parser.Nodes.Expressions;

/// <summary>
/// Represents a primary expression (identifier, literal, parenthesized expr, etc.)
/// </summary>
public sealed class PrimaryExprNode : ExprNode
{
    public PrimaryExprKind PrimaryKind { get; }
    public Literals.LiteralNode? Literal { get; }
    public string? Identifier { get; }
    public ExprNode? ParenthesizedExpr { get; }
    public CtOperatorExprNode? CtOperator { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public PrimaryExprNode(
        PrimaryExprKind kind,
        Literals.LiteralNode? literal,
        string? identifier,
        ExprNode? parenthesizedExpr,
        CtOperatorExprNode? ctOperator,
        TextSpan span,
        string fullText)
    {
        PrimaryKind = kind;
        Literal = literal;
        Identifier = identifier;
        ParenthesizedExpr = parenthesizedExpr;
        CtOperator = ctOperator;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.PrimaryExprNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Literal != null)
            yield return Literal;
        if (ParenthesizedExpr != null)
            yield return ParenthesizedExpr;
        if (CtOperator != null)
            yield return CtOperator;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        if (Identifier != null)
            writer.Write($" [{Identifier}]");
    }
}
