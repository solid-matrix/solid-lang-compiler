using SolidLang.Parser.Nodes.Expressions;

namespace SolidLang.Parser.Nodes.Types;

/// <summary>
/// Represents an array type: [expr] type
/// </summary>
public sealed class ArrayTypeNode : TypeNode
{
    public ExprNode Size { get; }
    public TypeNode ElementType { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ArrayTypeNode(ExprNode size, TypeNode elementType, TextSpan span, string fullText)
    {
        Size = size;
        ElementType = elementType;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ArrayTypeNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Size;
        yield return ElementType;
    }

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents a pointer type: *type or *!type
/// </summary>
public sealed class PointerTypeNode : TypeNode
{
    public bool HasWritePermission { get; }
    public TypeNode PointeeType { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public PointerTypeNode(bool hasWritePermission, TypeNode pointeeType, TextSpan span, string fullText)
    {
        HasWritePermission = hasWritePermission;
        PointeeType = pointeeType;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.PointerTypeNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return PointeeType;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        if (HasWritePermission)
            writer.Write(" [mutable]");
    }
}

/// <summary>
/// Represents a function pointer type: *func(...) call_conv: type
/// </summary>
public sealed class FuncPointerTypeNode : TypeNode
{
    public IReadOnlyList<TypeNode> ParameterTypes { get; }
    public Declarations.CallConventionNode? CallingConvention { get; }
    public TypeNode ReturnType { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public FuncPointerTypeNode(
        IReadOnlyList<TypeNode> parameterTypes,
        Declarations.CallConventionNode? callingConvention,
        TypeNode returnType,
        TextSpan span,
        string fullText)
    {
        ParameterTypes = parameterTypes;
        CallingConvention = callingConvention;
        ReturnType = returnType;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.FuncPointerTypeNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var p in ParameterTypes)
            yield return p;
        if (CallingConvention != null)
            yield return CallingConvention;
        yield return ReturnType;
    }

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents a named type (possibly with generic arguments): Name&lt;T1, T2&gt;
/// </summary>
public sealed class NamedTypeNode : TypeNode
{
    public Declarations.NamespacePrefixNode? NamespacePrefix { get; }
    public string Name { get; }
    public TypeArgumentListNode? TypeArguments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public NamedTypeNode(
        Declarations.NamespacePrefixNode? namespacePrefix,
        string name,
        TypeArgumentListNode? typeArguments,
        TextSpan span,
        string fullText)
    {
        NamespacePrefix = namespacePrefix;
        Name = name;
        TypeArguments = typeArguments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.NamedTypeNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (NamespacePrefix != null)
            yield return NamespacePrefix;
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
/// Represents type arguments: &lt;T1, T2, ...&gt;
/// </summary>
public sealed class TypeArgumentListNode : SyntaxNode
{
    public IReadOnlyList<TypeNode> Arguments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public TypeArgumentListNode(IReadOnlyList<TypeNode> arguments, TextSpan span, string fullText)
    {
        Arguments = arguments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.TypeArgumentListNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Arguments;

    public override string GetFullText() => _fullText;
}
