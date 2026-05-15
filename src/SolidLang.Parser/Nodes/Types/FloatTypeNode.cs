namespace SolidLang.Parser.Nodes.Types;

/// <summary>
/// Represents a float type: f32, f64
/// </summary>
public sealed class FloatTypeNode : TypeNode
{
    public SyntaxKind FloatKind { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public FloatTypeNode(SyntaxKind floatKind, TextSpan span, string fullText)
    {
        FloatKind = floatKind;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.FloatTypeNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{FloatKind}]");
    }
}
