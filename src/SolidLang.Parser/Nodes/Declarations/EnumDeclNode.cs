using SolidLang.Parser.Nodes.Expressions;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents an enum declaration: enum Name: type { fields }
/// </summary>
public sealed class EnumDeclNode : DeclNode
{
    public CtAnnotatesNode? Annotations { get; }
    public string Name { get; }
    public Types.TypeNode? UnderlyingType { get; }
    public EnumFieldsNode? Fields { get; }
    public bool IsForwardDecl { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public EnumDeclNode(
        CtAnnotatesNode? annotations,
        string name,
        Types.TypeNode? underlyingType,
        EnumFieldsNode? fields,
        bool isForwardDecl,
        TextSpan span,
        string fullText)
    {
        Annotations = annotations;
        Name = name;
        UnderlyingType = underlyingType;
        Fields = fields;
        IsForwardDecl = isForwardDecl;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.EnumDeclNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Annotations != null)
            yield return Annotations;
        if (UnderlyingType != null)
            yield return UnderlyingType;
        if (Fields != null)
            yield return Fields;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
