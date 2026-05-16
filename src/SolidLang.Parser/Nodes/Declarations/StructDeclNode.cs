using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents a struct declaration: struct Name&lt;T&gt; where T: Trait { fields }
/// </summary>
public sealed class StructDeclNode : DeclNode
{
    public IReadOnlyList<CtAnnotateNode> Annotations { get; }
    public string Name { get; }
    public IReadOnlyList<GenericParamNode> GenericParams { get; }
    public IReadOnlyList<WhereClauseNode> WhereClauses { get; }
    public IReadOnlyList<StructFieldNode> Fields { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public StructDeclNode(
        IReadOnlyList<CtAnnotateNode> annotations,
        string name,
        IReadOnlyList<GenericParamNode> genericParams,
        IReadOnlyList<WhereClauseNode> whereClauses,
        IReadOnlyList<StructFieldNode> fields,
        TextSpan span,
        string fullText)
    {
        Annotations = annotations;
        Name = name;
        GenericParams = genericParams;
        WhereClauses = whereClauses;
        Fields = fields;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.StructDeclNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var a in Annotations)
            yield return a;
        foreach (var p in GenericParams)
            yield return p;
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
