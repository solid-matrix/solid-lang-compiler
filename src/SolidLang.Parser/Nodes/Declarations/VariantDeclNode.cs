using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents a variant declaration: variant Name&lt;T&gt;: tag_type where T: Trait { fields }
/// </summary>
public sealed class VariantDeclNode : DeclNode
{
    public IReadOnlyList<CtAnnotateNode> Annotations { get; }
    public string Name { get; }
    public IReadOnlyList<GenericParamNode> GenericParams { get; }
    public TypeNode? TagType { get; }
    public IReadOnlyList<WhereClauseNode> WhereClauses { get; }
    public IReadOnlyList<VariantFieldNode> Fields { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public VariantDeclNode(
        IReadOnlyList<CtAnnotateNode> annotations,
        string name,
        IReadOnlyList<GenericParamNode> genericParams,
        TypeNode? tagType,
        IReadOnlyList<WhereClauseNode> whereClauses,
        IReadOnlyList<VariantFieldNode> fields,
        TextSpan span,
        string fullText)
    {
        Annotations = annotations;
        Name = name;
        GenericParams = genericParams;
        TagType = tagType;
        WhereClauses = whereClauses;
        Fields = fields;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.VariantDeclNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var a in Annotations)
            yield return a;
        foreach (var p in GenericParams)
            yield return p;
        if (TagType != null)
            yield return TagType;
        foreach (var w in WhereClauses)
            yield return w;
        foreach (var f in Fields)
            yield return f;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
