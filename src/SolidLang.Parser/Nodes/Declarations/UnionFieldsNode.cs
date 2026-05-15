namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents union fields: field1: type1, field2: type2, ...
/// </summary>
public sealed class UnionFieldsNode : SyntaxNode
{
    public IReadOnlyList<UnionFieldNode> Fields { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public UnionFieldsNode(IReadOnlyList<UnionFieldNode> fields, TextSpan span, string fullText)
    {
        Fields = fields;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.UnionFieldsNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Fields;

    public override string GetFullText() => _fullText;
}
