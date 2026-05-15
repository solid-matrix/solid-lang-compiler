namespace SolidLang.Parser.Nodes.Literals;

/// <summary>
/// Represents a string literal.
/// </summary>
public sealed class StringLiteralNode : LiteralNode
{
    public string Text { get; }
    public string Value { get; }
    private readonly TextSpan _span;

    public StringLiteralNode(string text, string value, TextSpan span)
    {
        Text = text;
        Value = value;
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.StringLiteralNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => Text;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [\"{Value}\"]");
    }
}
