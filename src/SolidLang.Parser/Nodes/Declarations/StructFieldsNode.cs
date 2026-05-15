namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents struct fields: field1: type1, field2: type2, ...
/// </summary>
public sealed class StructFieldsNode : SyntaxNode
{
    public IReadOnlyList<StructFieldNode> Fields { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public StructFieldsNode(IReadOnlyList<StructFieldNode> fields, TextSpan span, string fullText)
    {
        Fields = fields;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.StructFieldsNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Fields;

    public override string GetFullText() => _fullText;
}
