namespace SolidLang.Parser.Nodes.Expressions;

/// <summary>
/// Represents a pointer dereference member access: *.name (sugar for (*expr).name)
/// </summary>
public sealed class PointerAccessNode : PostfixSuffixNode
{
    public string Name { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public PointerAccessNode(string name, TextSpan span, string fullText)
    {
        Name = name;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.PointerAccessNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
