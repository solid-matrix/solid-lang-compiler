using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents a variant declaration: variant Name&lt;T&gt;: tag_type where T: Trait { fields }
/// </summary>
public sealed class VariantDeclNode : DeclNode
{
    public CtAnnotatesNode? Annotations { get; }
    public string Name { get; }
    public GenericParamsNode? GenericParams { get; }
    public Types.TypeNode? TagType { get; }
    public WhereClausesNode? WhereClauses { get; }
    public VariantFieldsNode? Fields { get; }
    public bool IsForwardDecl { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public VariantDeclNode(
        CtAnnotatesNode? annotations,
        string name,
        GenericParamsNode? genericParams,
        Types.TypeNode? tagType,
        WhereClausesNode? whereClauses,
        VariantFieldsNode? fields,
        bool isForwardDecl,
        TextSpan span,
        string fullText)
    {
        Annotations = annotations;
        Name = name;
        GenericParams = genericParams;
        TagType = tagType;
        WhereClauses = whereClauses;
        Fields = fields;
        IsForwardDecl = isForwardDecl;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.VariantDeclNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Annotations != null)
            yield return Annotations;
        if (GenericParams != null)
            yield return GenericParams;
        if (TagType != null)
            yield return TagType;
        if (WhereClauses != null)
            yield return WhereClauses;
        if (Fields != null)
            yield return Fields;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
