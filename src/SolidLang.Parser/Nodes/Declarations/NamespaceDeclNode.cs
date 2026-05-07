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

/// <summary>
/// Represents a namespace path: a::b::c
/// </summary>
public sealed class NamespacePathNode : SyntaxNode
{
    public IReadOnlyList<string> Segments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public NamespacePathNode(IReadOnlyList<string> segments, TextSpan span, string fullText)
    {
        Segments = segments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.NamespacePathNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{string.Join("::", Segments)}]");
    }
}

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
