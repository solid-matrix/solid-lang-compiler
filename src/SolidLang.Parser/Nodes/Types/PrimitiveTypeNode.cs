namespace SolidLang.Parser.Nodes.Types;

/// <summary>
/// Represents a primitive type: i32, f64, bool, etc.
/// </summary>
public sealed class PrimitiveTypeNode : TypeNode
{
    public SyntaxKind PrimitiveKind { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public PrimitiveTypeNode(SyntaxKind primitiveKind, TextSpan span, string fullText)
    {
        PrimitiveKind = primitiveKind;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.PrimitiveTypeNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{PrimitiveKind}]");
    }
}
