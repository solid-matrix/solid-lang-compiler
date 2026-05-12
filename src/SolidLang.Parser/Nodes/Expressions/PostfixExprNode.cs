using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Expressions;

/// <summary>
/// Represents a postfix expression: expr.suffix
/// </summary>
public sealed class PostfixExprNode : ExprNode
{
    public ExprNode Primary { get; }
    public IReadOnlyList<PostfixSuffixNode> Suffixes { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public PostfixExprNode(ExprNode primary, IReadOnlyList<PostfixSuffixNode> suffixes, TextSpan span, string fullText)
    {
        Primary = primary;
        Suffixes = suffixes;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.PostfixExprNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Primary;
        foreach (var suffix in Suffixes)
            yield return suffix;
    }

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Base class for postfix suffixes.
/// </summary>
public abstract class PostfixSuffixNode : SyntaxNode
{
}

/// <summary>
/// Represents a dot access: .name with optional generic type arguments (.name&lt;T&gt;)
/// </summary>
public sealed class DotAccessNode : PostfixSuffixNode
{
    public string Name { get; }
    public TypeArgumentListNode? TypeArguments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public DotAccessNode(string name, TypeArgumentListNode? typeArguments, TextSpan span, string fullText)
    {
        Name = name;
        TypeArguments = typeArguments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.DotAccessNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (TypeArguments != null)
            yield return TypeArguments;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}

/// <summary>
/// Represents an index access: [expr]
/// </summary>
public sealed class IndexAccessNode : PostfixSuffixNode
{
    public ExprNode Index { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public IndexAccessNode(ExprNode index, TextSpan span, string fullText)
    {
        Index = index;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.IndexAccessNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Index;
    }

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents a call expression: (args)
/// </summary>
public sealed class CallExprNode : PostfixSuffixNode
{
    public CallArgsNode? Arguments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CallExprNode(CallArgsNode? arguments, TextSpan span, string fullText)
    {
        Arguments = arguments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CallExprNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Arguments != null)
            yield return Arguments;
    }

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents a scope access: ::name or ::name(args)
/// </summary>
public sealed class ScopeAccessNode : PostfixSuffixNode
{
    public string Name { get; }
    public CallArgsNode? Arguments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ScopeAccessNode(string name, CallArgsNode? arguments, TextSpan span, string fullText)
    {
        Name = name;
        Arguments = arguments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ScopeAccessNode;
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
/// Represents a pointer dereference member access: *.name (sugar for (*expr).name)
/// </summary>
public sealed class PointerAccessNode : PostfixSuffixNode
{
    public string Name { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public PointerAccessNode(string name, TextSpan span, string fullText)
    {
        Name = name;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.PointerAccessNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}

/// <summary>
/// Represents an address-of member access: &.name (sugar for (&expr).name)
/// </summary>
public sealed class AddressAccessNode : PostfixSuffixNode
{
    public string Name { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public AddressAccessNode(string name, TextSpan span, string fullText)
    {
        Name = name;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.AddressAccessNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}

/// <summary>
/// Represents call arguments: arg1, arg2, ...
/// </summary>
public sealed class CallArgsNode : SyntaxNode
{
    public IReadOnlyList<CallArgNode> Arguments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CallArgsNode(IReadOnlyList<CallArgNode> arguments, TextSpan span, string fullText)
    {
        Arguments = arguments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CallArgsNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Arguments;

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents a call argument.
/// </summary>
public sealed class CallArgNode : SyntaxNode
{
    public ExprNode Expression { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CallArgNode(ExprNode expression, TextSpan span, string fullText)
    {
        Expression = expression;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CallArgNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
    }

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents a primary expression (identifier, literal, parenthesized expr, etc.)
/// </summary>
public sealed class PrimaryExprNode : ExprNode
{
    public PrimaryExprKind PrimaryKind { get; }
    public Literals.LiteralNode? Literal { get; }
    public string? Identifier { get; }
    public ExprNode? ParenthesizedExpr { get; }
    public CtOperatorExprNode? CtOperator { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public PrimaryExprNode(
        PrimaryExprKind kind,
        Literals.LiteralNode? literal,
        string? identifier,
        ExprNode? parenthesizedExpr,
        CtOperatorExprNode? ctOperator,
        TextSpan span,
        string fullText)
    {
        PrimaryKind = kind;
        Literal = literal;
        Identifier = identifier;
        ParenthesizedExpr = parenthesizedExpr;
        CtOperator = ctOperator;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.PrimaryExprNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Literal != null)
            yield return Literal;
        if (ParenthesizedExpr != null)
            yield return ParenthesizedExpr;
        if (CtOperator != null)
            yield return CtOperator;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        if (Identifier != null)
            writer.Write($" [{Identifier}]");
    }
}

public enum PrimaryExprKind
{
    Literal,
    Identifier,
    Parenthesized,
    CtOperator,
}

/// <summary>
/// Represents a compile-time operator expression: @name(args)
/// </summary>
public sealed class CtOperatorExprNode : ExprNode
{
    public string Name { get; }
    public CtOperatorArgsNode? Arguments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CtOperatorExprNode(string name, CtOperatorArgsNode? arguments, TextSpan span, string fullText)
    {
        Name = name;
        Arguments = arguments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CtOperatorExprNode;
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
/// Represents compile-time operator arguments.
/// </summary>
public sealed class CtOperatorArgsNode : SyntaxNode
{
    public IReadOnlyList<CtOperatorArgNode> Arguments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CtOperatorArgsNode(IReadOnlyList<CtOperatorArgNode> arguments, TextSpan span, string fullText)
    {
        Arguments = arguments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CtOperatorArgsNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Arguments;

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents a compile-time operator argument (type or expression).
/// </summary>
public sealed class CtOperatorArgNode : SyntaxNode
{
    public TypeNode? Type { get; }
    public ExprNode? Expression { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CtOperatorArgNode(TypeNode? type, ExprNode? expression, TextSpan span, string fullText)
    {
        Type = type;
        Expression = expression;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CtOperatorArgNode;
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
