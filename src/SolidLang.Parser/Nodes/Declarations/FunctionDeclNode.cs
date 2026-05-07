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
    public NamespacePrefixNode? NamespacePrefix { get; }
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
        NamespacePrefixNode? namespacePrefix,
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
        NamespacePrefix = namespacePrefix;
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
        if (NamespacePrefix != null)
            yield return NamespacePrefix;
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

/// <summary>
/// Represents a namespace prefix: a::b::
/// </summary>
public sealed class NamespacePrefixNode : SyntaxNode
{
    public NamespacePathNode Path { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public NamespacePrefixNode(NamespacePathNode path, TextSpan span, string fullText)
    {
        Path = path;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.NamespacePrefixNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Path;
    }

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents generic parameters: &lt;T, U, V&gt;
/// </summary>
public sealed class GenericParamsNode : SyntaxNode
{
    public IReadOnlyList<GenericParamNode> Parameters { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public GenericParamsNode(IReadOnlyList<GenericParamNode> parameters, TextSpan span, string fullText)
    {
        Parameters = parameters;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.GenericParamsNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Parameters;

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents a generic parameter: T
/// </summary>
public sealed class GenericParamNode : SyntaxNode
{
    public string Name { get; }
    private readonly TextSpan _span;

    public GenericParamNode(string name, TextSpan span)
    {
        Name = name;
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.GenericParamNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => Name;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}

/// <summary>
/// Represents where clauses: where T: Trait where U: AnotherTrait
/// </summary>
public sealed class WhereClausesNode : SyntaxNode
{
    public IReadOnlyList<WhereClauseNode> Clauses { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public WhereClausesNode(IReadOnlyList<WhereClauseNode> clauses, TextSpan span, string fullText)
    {
        Clauses = clauses;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.WhereClausesNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Clauses;

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents a where clause: where T: Trait
/// </summary>
public sealed class WhereClauseNode : SyntaxNode
{
    public string TypeParamName { get; }
    public Types.TypeNode ConstraintType { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public WhereClauseNode(string typeParamName, Types.TypeNode constraintType, TextSpan span, string fullText)
    {
        TypeParamName = typeParamName;
        ConstraintType = constraintType;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.WhereClauseNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return ConstraintType;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{TypeParamName}]");
    }
}

/// <summary>
/// Represents compile-time annotations: @attr1 @attr2(args)
/// </summary>
public sealed class CtAnnotatesNode : SyntaxNode
{
    public IReadOnlyList<CtAnnotateNode> Annotations { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CtAnnotatesNode(IReadOnlyList<CtAnnotateNode> annotations, TextSpan span, string fullText)
    {
        Annotations = annotations;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CtAnnotatesNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Annotations;

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents a compile-time annotation: @name or @name(args)
/// </summary>
public sealed class CtAnnotateNode : SyntaxNode
{
    public string Name { get; }
    public CtAnnotateArgsNode? Arguments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CtAnnotateNode(string name, CtAnnotateArgsNode? arguments, TextSpan span, string fullText)
    {
        Name = name;
        Arguments = arguments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CtAnnotateNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Arguments != null)
            yield return Arguments;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}

/// <summary>
/// Represents annotation arguments: arg1, arg2, ...
/// </summary>
public sealed class CtAnnotateArgsNode : SyntaxNode
{
    public IReadOnlyList<CtAnnotateArgNode> Arguments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CtAnnotateArgsNode(IReadOnlyList<CtAnnotateArgNode> arguments, TextSpan span, string fullText)
    {
        Arguments = arguments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CtAnnotateArgsNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Arguments;

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents an annotation argument (type or expression).
/// </summary>
public sealed class CtAnnotateArgNode : SyntaxNode
{
    public Types.TypeNode? Type { get; }
    public ExprNode? Expression { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CtAnnotateArgNode(Types.TypeNode? type, ExprNode? expression, TextSpan span, string fullText)
    {
        Type = type;
        Expression = expression;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CtAnnotateArgNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Type != null)
            yield return Type;
        if (Expression != null)
            yield return Expression;
    }

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents a calling convention: cdecl or stdcall
/// </summary>
public sealed class CallConventionNode : SyntaxNode
{
    public SyntaxKind Convention { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CallConventionNode(SyntaxKind convention, TextSpan span, string fullText)
    {
        Convention = convention;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CallConventionNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Convention}]");
    }
}

/// <summary>
/// Represents function parameters: (param1: type1, param2: type2)
/// </summary>
public sealed class FuncParametersNode : SyntaxNode
{
    public IReadOnlyList<FuncParameterNode> Parameters { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public FuncParametersNode(IReadOnlyList<FuncParameterNode> parameters, TextSpan span, string fullText)
    {
        Parameters = parameters;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.FuncParametersNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Parameters;

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents a function parameter: name: type
/// </summary>
public sealed class FuncParameterNode : SyntaxNode
{
    public CtAnnotatesNode? Annotations { get; }
    public string Name { get; }
    public Types.TypeNode Type { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public FuncParameterNode(CtAnnotatesNode? annotations, string name, Types.TypeNode type, TextSpan span, string fullText)
    {
        Annotations = annotations;
        Name = name;
        Type = type;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.FuncParameterNode;
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
