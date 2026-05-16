using SolidLang.Parser.Nodes.Expressions;
using SolidLang.Parser.Nodes.Statements;
using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents a function declaration: func name&lt;T&gt;(params) call_conv: type where T: Trait { body }
/// </summary>
public sealed class FunctionDeclNode : DeclNode
{
    public IReadOnlyList<CtAnnotateNode> Annotations { get; }
    public NamedTypeNode? NamedTypePrefix { get; }
    public string Name { get; }
    public IReadOnlyList<GenericParamNode> GenericParams { get; }
    public IReadOnlyList<FuncParameterNode> Parameters { get; }
    public CallConventionNode? CallingConvention { get; }
    public Types.TypeNode? ReturnType { get; }
    public IReadOnlyList<WhereClauseNode> WhereClauses { get; }
    public BodyStmtNode? Body { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public FunctionDeclNode(
        IReadOnlyList<CtAnnotateNode> annotations,
        NamedTypeNode? namedTypePrefix,
        string name,
        IReadOnlyList<GenericParamNode> genericParams,
        IReadOnlyList<FuncParameterNode> parameters,
        CallConventionNode? callingConvention,
        Types.TypeNode? returnType,
        IReadOnlyList<WhereClauseNode> whereClauses,
        BodyStmtNode? body,
        TextSpan span,
        string fullText)
    {
        Annotations = annotations;
        NamedTypePrefix = namedTypePrefix;
        Name = name;
        GenericParams = genericParams;
        Parameters = parameters;
        CallingConvention = callingConvention;
        ReturnType = returnType;
        WhereClauses = whereClauses;
        Body = body;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.FunctionDeclNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var a in Annotations)
            yield return a;
        if (NamedTypePrefix != null)
            yield return NamedTypePrefix;
        foreach (var p in GenericParams)
            yield return p;
        foreach (var p in Parameters)
            yield return p;
        if (CallingConvention != null)
            yield return CallingConvention;
        if (ReturnType != null)
            yield return ReturnType;
        foreach (var w in WhereClauses)
            yield return w;
        if (Body != null)
            yield return Body;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
