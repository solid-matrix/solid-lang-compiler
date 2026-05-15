namespace SolidLang.Parser.Nodes.Expressions;

/// <summary>
/// Represents a compile-time operator expression: @name(args)
/// </summary>
public sealed class CtOperatorExprNode : ExprNode
{
    public string Name { get; }
    public CtOperatorArgsNode? Arguments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CtOperatorExprNode(string name, CtOperatorArgsNode? arguments, TextSpan span, string fullText)
    {
        Name = name;
        Arguments = arguments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CtOperatorExprNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Arguments != null)
            yield return Arguments;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
