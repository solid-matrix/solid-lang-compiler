using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents a union field: name: type
/// </summary>
public sealed class UnionFieldNode : SyntaxNode
{
    public IReadOnlyList<CtAnnotateNode> Annotations { get; }
    public string Name { get; }
    public TypeNode Type { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public UnionFieldNode(IReadOnlyList<CtAnnotateNode> annotations, string name, TypeNode type, TextSpan span, string fullText)
    {
        Annotations = annotations;
        Name = name;
        Type = type;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.UnionFieldNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var a in Annotations)
            yield return a;
        yield return Type;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
