using SolidLang.Parser.Nodes.Expressions;
using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents a variable declaration: var/const/static [Type::]name: type = expr;
/// </summary>
public sealed class VariableDeclNode : DeclNode
{
    public IReadOnlyList<CtAnnotateNode> Annotations { get; }
    public SyntaxKind Keyword { get; }
    public NamedTypeNode? NamedTypePrefix { get; }
    public string Name { get; }
    public TypeNode? Type { get; }
    public ExprNode? Initializer { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public VariableDeclNode(
        IReadOnlyList<CtAnnotateNode> annotations,
        SyntaxKind keyword,
        NamedTypeNode? namedTypePrefix,
        string name,
        TypeNode? type,
        ExprNode? initializer,
        TextSpan span,
        string fullText)
    {
        Annotations = annotations;
        Keyword = keyword;
        NamedTypePrefix = namedTypePrefix;
        Name = name;
        Type = type;
        Initializer = initializer;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.VariableDeclNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var a in Annotations)
            yield return a;
        if (NamedTypePrefix != null)
            yield return NamedTypePrefix;
        if (Type != null)
            yield return Type;
        if (Initializer != null)
            yield return Initializer;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Keyword} {Name}]");
    }
}
