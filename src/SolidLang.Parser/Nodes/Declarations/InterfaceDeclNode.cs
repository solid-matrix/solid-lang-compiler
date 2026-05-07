using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents an interface declaration: interface Name&lt;T&gt; where T: Trait { fields }
/// </summary>
public sealed class InterfaceDeclNode : DeclNode
{
    public CtAnnotatesNode? Annotations { get; }
    public string Name { get; }
    public GenericParamsNode? GenericParams { get; }
    public WhereClausesNode? WhereClauses { get; }
    public InterfaceFieldsNode? Fields { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public InterfaceDeclNode(
        CtAnnotatesNode? annotations,
        string name,
        GenericParamsNode? genericParams,
        WhereClausesNode? whereClauses,
        InterfaceFieldsNode? fields,
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
/// Represents interface fields (method signatures).
/// </summary>
public sealed class InterfaceFieldsNode : SyntaxNode
{
    public IReadOnlyList<InterfaceFieldNode> Fields { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public InterfaceFieldsNode(IReadOnlyList<InterfaceFieldNode> fields, TextSpan span, string fullText)
    {
        Fields = fields;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.InterfaceFieldsNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Fields;

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents an interface field (method signature): func name&lt;T&gt;(params): type
/// </summary>
public sealed class InterfaceFieldNode : SyntaxNode
{
    public CtAnnotatesNode? Annotations { get; }
    public string Name { get; }
    public GenericParamsNode? GenericParams { get; }
    public FuncParametersNode? Parameters { get; }
    public Types.TypeNode? ReturnType { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public InterfaceFieldNode(
        CtAnnotatesNode? annotations,
        string name,
        GenericParamsNode? genericParams,
        FuncParametersNode? parameters,
        Types.TypeNode? returnType,
        TextSpan span,
        string fullText)
    {
        Annotations = annotations;
        Name = name;
        GenericParams = genericParams;
        Parameters = parameters;
        ReturnType = returnType;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.InterfaceFieldNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Annotations != null)
            yield return Annotations;
        if (GenericParams != null)
            yield return GenericParams;
        if (Parameters != null)
            yield return Parameters;
        if (ReturnType != null)
            yield return ReturnType;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
