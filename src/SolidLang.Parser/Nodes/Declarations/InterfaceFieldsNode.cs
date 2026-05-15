namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents interface fields (method signatures).
/// </summary>
public sealed class InterfaceFieldsNode : SyntaxNode
{
    public IReadOnlyList<InterfaceFieldNode> Fields { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public InterfaceFieldsNode(IReadOnlyList<InterfaceFieldNode> fields, TextSpan span, string fullText)
    {
        Fields = fields;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.InterfaceFieldsNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Fields;

    public override string GetFullText() => _fullText;
}
