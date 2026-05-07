using SolidLang.Parser.Nodes.Expressions;
using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Literals;

/// <summary>
/// Represents an array literal: [size]type{ elements }
/// </summary>
public sealed class ArrayLiteralNode : LiteralNode
{
    public ArrayTypeNode ArrayType { get; }
    public IReadOnlyList<ExprNode> Elements { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ArrayLiteralNode(ArrayTypeNode arrayType, IReadOnlyList<ExprNode> elements, TextSpan span, string fullText)
    {
        ArrayType = arrayType;
        Elements = elements;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ArrayLiteralNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return ArrayType;
        foreach (var e in Elements)
            yield return e;
    }

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents a struct literal: Name{ fields }
/// </summary>
public sealed class StructLiteralNode : LiteralNode
{
    public NamedTypeNode StructType { get; }
    public IReadOnlyList<StructLiteralFieldNode> Fields { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public StructLiteralNode(NamedTypeNode structType, IReadOnlyList<StructLiteralFieldNode> fields, TextSpan span, string fullText)
    {
        StructType = structType;
        Fields = fields;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.StructLiteralNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return StructType;
        foreach (var f in Fields)
            yield return f;
    }

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents a struct literal field: name = expr
/// </summary>
public sealed class StructLiteralFieldNode : SyntaxNode
{
    public string Name { get; }
    public ExprNode Value { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public StructLiteralFieldNode(string name, ExprNode value, TextSpan span, string fullText)
    {
        Name = name;
        Value = value;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.StructLiteralFieldNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Value;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}

/// <summary>
/// Represents a union literal: Name::member(expr)
/// </summary>
public sealed class UnionLiteralNode : LiteralNode
{
    public NamedTypeNode UnionType { get; }
    public string MemberName { get; }
    public ExprNode? Value { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public UnionLiteralNode(NamedTypeNode unionType, string memberName, ExprNode? value, TextSpan span, string fullText)
    {
        UnionType = unionType;
        MemberName = memberName;
        Value = value;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.UnionLiteralNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return UnionType;
        if (Value != null)
            yield return Value;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{MemberName}]");
    }
}

/// <summary>
/// Represents an enum literal: Name::member
/// </summary>
public sealed class EnumLiteralNode : LiteralNode
{
    public NamedTypeNode EnumType { get; }
    public string MemberName { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public EnumLiteralNode(NamedTypeNode enumType, string memberName, TextSpan span, string fullText)
    {
        EnumType = enumType;
        MemberName = memberName;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.EnumLiteralNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return EnumType;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{MemberName}]");
    }
}

/// <summary>
/// Represents a variant literal: Name&lt;T&gt;::member(expr?)
/// </summary>
public sealed class VariantLiteralNode : LiteralNode
{
    public NamedTypeNode VariantType { get; }
    public string MemberName { get; }
    public ExprNode? Value { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public VariantLiteralNode(NamedTypeNode variantType, string memberName, ExprNode? value, TextSpan span, string fullText)
    {
        VariantType = variantType;
        MemberName = memberName;
        Value = value;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.VariantLiteralNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return VariantType;
        if (Value != null)
            yield return Value;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{MemberName}]");
    }
}
