namespace SolidLang.Parser.Nodes.Literals;

/// <summary>
/// Represents a character literal.
/// </summary>
public sealed class CharLiteralNode : LiteralNode
{
    public string Text { get; }
    public string Value { get; }
    private readonly TextSpan _span;

    public CharLiteralNode(string text, string value, TextSpan span)
    {
        Text = text;
        Value = value;
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.CharLiteralNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => Text;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" ['{Value}']");
    }
}
