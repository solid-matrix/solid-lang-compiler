namespace SolidLang.Parser.Nodes.Literals;

/// <summary>
/// Represents an integer literal.
/// </summary>
public sealed class IntegerLiteralNode : LiteralNode
{
    public string Text { get; }
    public object Value { get; }
    public SyntaxKind? TypeSuffix { get; }
    private readonly TextSpan _span;

    public IntegerLiteralNode(string text, object value, SyntaxKind? typeSuffix, TextSpan span)
    {
        Text = text;
        Value = value;
        TypeSuffix = typeSuffix;
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.IntegerLiteralNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => Text;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Value}]");
    }
}
