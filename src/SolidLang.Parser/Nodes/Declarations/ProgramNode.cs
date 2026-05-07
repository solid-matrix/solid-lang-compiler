namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents the root node of a Solid program.
/// </summary>
public sealed class ProgramNode : SyntaxNode
{
    public NamespaceDeclNode? Namespace { get; }
    public IReadOnlyList<UsingDeclNode> Usings { get; }
    public IReadOnlyList<DeclNode> Declarations { get; }
    private readonly SourceText _source;
    private readonly TextSpan _span;

    public ProgramNode(NamespaceDeclNode? ns, IReadOnlyList<UsingDeclNode> usings, IReadOnlyList<DeclNode> decls, SourceText source, TextSpan span)
    {
        Namespace = ns;
        Usings = usings;
        Declarations = decls;
        _source = source;
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.ProgramNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Namespace != null)
            yield return Namespace;

        foreach (var u in Usings)
            yield return u;

        foreach (var d in Declarations)
            yield return d;
    }

    public override string GetFullText() => _source.GetText(_span);
}
