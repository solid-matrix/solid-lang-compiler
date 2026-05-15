using SolidLang.Parser.Nodes.Expressions;
using SolidLang.Parser.Nodes.Statements;
using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents a function declaration: func name&lt;T&gt;(params) call_conv: type where T: Trait { body }
/// </summary>
public sealed class FunctionDeclNode : DeclNode
{
    public CtAnnotatesNode? Annotations { get; }
    public NamedTypeSpacePrefixNode? NamedTypePrefix { get; }
    public string Name { get; }
    public GenericParamsNode? GenericParams { get; }
    public FuncParametersNode? Parameters { get; }
    public CallConventionNode? CallingConvention { get; }
    public Types.TypeNode? ReturnType { get; }
    public WhereClausesNode? WhereClauses { get; }
    public BodyStmtNode? Body { get; }
    public bool IsForwardDecl { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public FunctionDeclNode(
        CtAnnotatesNode? annotations,
        NamedTypeSpacePrefixNode? namedTypePrefix,
        string name,
        GenericParamsNode? genericParams,
        FuncParametersNode? parameters,
        CallConventionNode? callingConvention,
        Types.TypeNode? returnType,
        WhereClausesNode? whereClauses,
        BodyStmtNode? body,
        bool isForwardDecl,
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
        IsForwardDecl = isForwardDecl;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.FunctionDeclNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Annotations != null)
            yield return Annotations;
        if (NamedTypePrefix != null)
            yield return NamedTypePrefix;
        if (GenericParams != null)
            yield return GenericParams;
        if (Parameters != null)
            yield return Parameters;
        if (CallingConvention != null)
            yield return CallingConvention;
        if (ReturnType != null)
            yield return ReturnType;
        if (WhereClauses != null)
            yield return WhereClauses;
        if (Body != null)
            yield return Body;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
