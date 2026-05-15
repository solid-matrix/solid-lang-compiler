namespace SolidLang.Parser.Nodes.Literals;

/// <summary>
/// Represents a boolean literal: true or false.
/// </summary>
public sealed class BoolLiteralNode : LiteralNode
{
    public bool Value { get; }
    private readonly TextSpan _span;

    public BoolLiteralNode(bool value, TextSpan span)
    {
        Value = value;
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.BoolLiteralNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => Value ? "true" : "false";

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Value}]");
    }
}
