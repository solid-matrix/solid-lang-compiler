namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents variant fields: Field1: type1, Field2, ...
/// </summary>
public sealed class VariantFieldsNode : SyntaxNode
{
    public IReadOnlyList<VariantFieldNode> Fields { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public VariantFieldsNode(IReadOnlyList<VariantFieldNode> fields, TextSpan span, string fullText)
    {
        Fields = fields;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.VariantFieldsNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Fields;

    public override string GetFullText() => _fullText;
}
