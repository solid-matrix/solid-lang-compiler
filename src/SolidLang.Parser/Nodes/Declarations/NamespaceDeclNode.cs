namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents a namespace declaration: namespace path;
/// </summary>
public sealed class NamespaceDeclNode : DeclNode
{
    public NamespacePathNode Path { get; }
    private readonly TextSpan _span;

    public NamespaceDeclNode(NamespacePathNode path, TextSpan span)
    {
        Path = path;
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.NamespaceDeclNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Path;
    }

    public override string GetFullText() => $"namespace {Path.GetFullText()};";
}
