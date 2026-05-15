namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents a using declaration: using path;
/// </summary>
public sealed class UsingDeclNode : DeclNode
{
    public NamespacePathNode Path { get; }
    private readonly TextSpan _span;

    public UsingDeclNode(NamespacePathNode path, TextSpan span)
    {
        Path = path;
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.UsingDeclNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Path;
    }

    public override string GetFullText() => $"using {Path.GetFullText()};";
}
