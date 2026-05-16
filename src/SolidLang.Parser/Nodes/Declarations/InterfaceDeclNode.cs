using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents an interface declaration: interface Name&lt;T&gt; where T: Trait { fields }
/// </summary>
public sealed class InterfaceDeclNode : DeclNode
{
    public IReadOnlyList<CtAnnotateNode> Annotations { get; }
    public string Name { get; }
    public IReadOnlyList<GenericParamNode> GenericParams { get; }
    public IReadOnlyList<WhereClauseNode> WhereClauses { get; }
    public IReadOnlyList<InterfaceFieldNode> Fields { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public InterfaceDeclNode(
        IReadOnlyList<CtAnnotateNode> annotations,
        string name,
        IReadOnlyList<GenericParamNode> genericParams,
        IReadOnlyList<WhereClauseNode> whereClauses,
        IReadOnlyList<InterfaceFieldNode> fields,
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

    public override SyntaxKind Kind => SyntaxKind.InterfaceDeclNode;
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
