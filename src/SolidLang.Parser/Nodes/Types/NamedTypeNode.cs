namespace SolidLang.Parser.Nodes.Types;

/// <summary>
/// Represents a named type (possibly with generic arguments): Name&lt;T1, T2&gt;
/// </summary>
public sealed class NamedTypeNode : TypeNode
{
    public Declarations.NamespacePrefixNode? NamespacePrefix { get; }
    public string Name { get; }
    public IReadOnlyList<TypeNode> TypeArguments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public NamedTypeNode(
        Declarations.NamespacePrefixNode? namespacePrefix,
        string name,
        IReadOnlyList<TypeNode> typeArguments,
        TextSpan span,
        string fullText)
    {
        NamespacePrefix = namespacePrefix;
        Name = name;
        TypeArguments = typeArguments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.NamedTypeNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (NamespacePrefix != null)
            yield return NamespacePrefix;
        foreach (var t in TypeArguments)
            yield return t;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
