using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents a variant field: Name: type or just Name
/// </summary>
public sealed class VariantFieldNode : SyntaxNode
{
    public CtAnnotatesNode? Annotations { get; }
    public string Name { get; }
    public Types.TypeNode? Type { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public VariantFieldNode(CtAnnotatesNode? annotations, string name, Types.TypeNode? type, TextSpan span, string fullText)
    {
        Annotations = annotations;
        Name = name;
        Type = type;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.VariantFieldNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Annotations != null)
            yield return Annotations;
        if (Type != null)
            yield return Type;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
