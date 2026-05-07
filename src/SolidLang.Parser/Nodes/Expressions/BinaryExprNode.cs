namespace SolidLang.Parser.Nodes.Expressions;

/// <summary>
/// Represents a conditional (ternary) expression: cond ? then : else
/// </summary>
public sealed class ConditionalExprNode : ExprNode
{
    public ExprNode Condition { get; }
    public ExprNode ThenExpr { get; }
    public ExprNode ElseExpr { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ConditionalExprNode(ExprNode condition, ExprNode thenExpr, ExprNode elseExpr, TextSpan span, string fullText)
    {
        Condition = condition;
        ThenExpr = thenExpr;
        ElseExpr = elseExpr;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ConditionalExprNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Condition;
        yield return ThenExpr;
        yield return ElseExpr;
    }

    public override string GetFullText() => _fullText;
}

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
        _ => SyntaxKind.AddExprNode,
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
