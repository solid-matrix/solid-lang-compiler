namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents a namespace prefix: a::b::
/// </summary>
public sealed class NamespacePrefixNode : SyntaxNode
{
    public NamespacePathNode Path { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public NamespacePrefixNode(NamespacePathNode path, TextSpan span, string fullText)
    {
        Path = path;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.NamespacePrefixNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Path;
    }

    public override string GetFullText() => _fullText;
}
