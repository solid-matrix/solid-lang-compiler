namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents enum fields: Field1 = value, Field2, ...
/// </summary>
public sealed class EnumFieldsNode : SyntaxNode
{
    public IReadOnlyList<EnumFieldNode> Fields { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public EnumFieldsNode(IReadOnlyList<EnumFieldNode> fields, TextSpan span, string fullText)
    {
        Fields = fields;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.EnumFieldsNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Fields;

    public override string GetFullText() => _fullText;
}
