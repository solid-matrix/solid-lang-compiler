namespace SolidLang.Parser.Nodes.Expressions;

/// <summary>
/// Represents a binary expression: left op right
/// </summary>
public sealed class BinaryExprNode : ExprNode
{
    public ExprNode Left { get; }
    public SyntaxKind Operator { get; }
    public ExprNode Right { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public BinaryExprNode(ExprNode left, SyntaxKind op, ExprNode right, TextSpan span, string fullText)
    {
        Left = left;
        Operator = op;
        Right = right;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => Operator switch
    {
        SyntaxKind.PipePipeToken => SyntaxKind.OrExprNode,
        SyntaxKind.AmpersandAmpersandToken => SyntaxKind.AndExprNode,
        SyntaxKind.PipeToken => SyntaxKind.BitOrExprNode,
        SyntaxKind.CaretToken => SyntaxKind.BitXorExprNode,
        SyntaxKind.AmpersandToken => SyntaxKind.BitAndExprNode,
        SyntaxKind.EqualsEqualsToken or SyntaxKind.BangEqualsToken => SyntaxKind.EqualityExprNode,
        SyntaxKind.LessToken or SyntaxKind.GreaterToken or SyntaxKind.LessEqualsToken or SyntaxKind.GreaterEqualsToken => SyntaxKind.ComparisonExprNode,
        SyntaxKind.LessLessToken or SyntaxKind.GreaterGreaterToken => SyntaxKind.ShiftExprNode,
        SyntaxKind.PlusToken or SyntaxKind.MinusToken => SyntaxKind.AddExprNode,
        SyntaxKind.StarToken or SyntaxKind.SlashToken or SyntaxKind.PercentToken => SyntaxKind.MulExprNode,
        SyntaxKind.EqualsToken or SyntaxKind.PlusEqualsToken or SyntaxKind.MinusEqualsToken or SyntaxKind.StarEqualsToken or SyntaxKind.SlashEqualsToken or SyntaxKind.PercentEqualsToken or SyntaxKind.AmpersandEqualsToken or SyntaxKind.PipeEqualsToken or SyntaxKind.CaretEqualsToken or SyntaxKind.LessLessEqualsToken or SyntaxKind.GreaterGreaterEqualsToken => SyntaxKind.AssignExprNode,
        _ => SyntaxKind.AssignExprNode,
    };

    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Left;
        yield return Right;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Operator}]");
    }
}
