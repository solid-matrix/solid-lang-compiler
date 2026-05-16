using SolidLang.Parser.Nodes.Expressions;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents an enum declaration: enum Name: type { fields }
/// </summary>
public sealed class EnumDeclNode : DeclNode
{
    public IReadOnlyList<CtAnnotateNode> Annotations { get; }
    public string Name { get; }
    public Types.TypeNode? UnderlyingType { get; }
    public IReadOnlyList<EnumFieldNode> Fields { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public EnumDeclNode(
        IReadOnlyList<CtAnnotateNode> annotations,
        string name,
        Types.TypeNode? underlyingType,
        IReadOnlyList<EnumFieldNode> fields,
        TextSpan span,
        string fullText)
    {
        Annotations = annotations;
        Name = name;
        UnderlyingType = underlyingType;
        Fields = fields;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.EnumDeclNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var a in Annotations)
            yield return a;
        if (UnderlyingType != null)
            yield return UnderlyingType;
        foreach (var f in Fields)
            yield return f;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
