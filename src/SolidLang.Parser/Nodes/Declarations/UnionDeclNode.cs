using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents a union declaration: union Name&lt;T&gt; where T: Trait { fields }
/// </summary>
public sealed class UnionDeclNode : DeclNode
{
    public CtAnnotatesNode? Annotations { get; }
    public string Name { get; }
    public GenericParamsNode? GenericParams { get; }
    public WhereClausesNode? WhereClauses { get; }
    public UnionFieldsNode? Fields { get; }
    public bool IsForwardDecl { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public UnionDeclNode(
        CtAnnotatesNode? annotations,
        string name,
        GenericParamsNode? genericParams,
        WhereClausesNode? whereClauses,
        UnionFieldsNode? fields,
        bool isForwardDecl,
        TextSpan span,
        string fullText)
    {
        Annotations = annotations;
        Name = name;
        GenericParams = genericParams;
        WhereClauses = whereClauses;
        Fields = fields;
        IsForwardDecl = isForwardDecl;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.UnionDeclNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Annotations != null)
            yield return Annotations;
        if (GenericParams != null)
            yield return GenericParams;
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

/// <summary>
/// Represents union fields: field1: type1, field2: type2, ...
/// </summary>
public sealed class UnionFieldsNode : SyntaxNode
{
    public IReadOnlyList<UnionFieldNode> Fields { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public UnionFieldsNode(IReadOnlyList<UnionFieldNode> fields, TextSpan span, string fullText)
    {
        Fields = fields;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.UnionFieldsNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Fields;

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents a union field: name: type
/// </summary>
public sealed class UnionFieldNode : SyntaxNode
{
    public CtAnnotatesNode? Annotations { get; }
    public string Name { get; }
    public TypeNode Type { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public UnionFieldNode(CtAnnotatesNode? annotations, string name, TypeNode type, TextSpan span, string fullText)
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
        if (Annotations != null)
            yield return Annotations;
        yield return Type;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
