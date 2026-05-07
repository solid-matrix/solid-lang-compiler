using SolidLang.Parser.Nodes.Expressions;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents an enum declaration: enum Name: type { fields }
/// </summary>
public sealed class EnumDeclNode : DeclNode
{
    public CtAnnotatesNode? Annotations { get; }
    public string Name { get; }
    public Types.TypeNode? UnderlyingType { get; }
    public EnumFieldsNode? Fields { get; }
    public bool IsForwardDecl { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public EnumDeclNode(
        CtAnnotatesNode? annotations,
        string name,
        Types.TypeNode? underlyingType,
        EnumFieldsNode? fields,
        bool isForwardDecl,
        TextSpan span,
        string fullText)
    {
        Annotations = annotations;
        Name = name;
        UnderlyingType = underlyingType;
        Fields = fields;
        IsForwardDecl = isForwardDecl;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.EnumDeclNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Annotations != null)
            yield return Annotations;
        if (UnderlyingType != null)
            yield return UnderlyingType;
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
/// Represents enum fields: Field1 = value, Field2, ...
/// </summary>
public sealed class EnumFieldsNode : SyntaxNode
{
    public IReadOnlyList<EnumFieldNode> Fields { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public EnumFieldsNode(IReadOnlyList<EnumFieldNode> fields, TextSpan span, string fullText)
    {
        Fields = fields;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.EnumFieldsNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Fields;

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents an enum field: Name = value or just Name
/// </summary>
public sealed class EnumFieldNode : SyntaxNode
{
    public CtAnnotatesNode? Annotations { get; }
    public string Name { get; }
    public ExprNode? Value { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public EnumFieldNode(CtAnnotatesNode? annotations, string name, ExprNode? value, TextSpan span, string fullText)
    {
        Annotations = annotations;
        Name = name;
        Value = value;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.EnumFieldNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Annotations != null)
            yield return Annotations;
        if (Value != null)
            yield return Value;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
