using SolidLang.Parser.Nodes.Expressions;
using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents a constant declaration: const [Type::]name: type = expr;
/// </summary>
public sealed class ConstDeclNode : DeclNode
{
    public CtAnnotatesNode? Annotations { get; }
    public NamedTypeSpacePrefixNode? NamedTypePrefix { get; }
    public string Name { get; }
    public TypeNode? Type { get; }
    public ExprNode? Initializer { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ConstDeclNode(CtAnnotatesNode? annotations, NamedTypeSpacePrefixNode? namedTypePrefix, string name, TypeNode? type, ExprNode? initializer, TextSpan span, string fullText)
    {
        Annotations = annotations;
        NamedTypePrefix = namedTypePrefix;
        Name = name;
        Type = type;
        Initializer = initializer;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ConstDeclNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Annotations != null)
            yield return Annotations;
        if (NamedTypePrefix != null)
            yield return NamedTypePrefix;
        if (Type != null)
            yield return Type;
        if (Initializer != null)
            yield return Initializer;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}

/// <summary>
/// Represents a static variable declaration: static [Type::]name: type = expr;
/// </summary>
public sealed class StaticDeclNode : DeclNode
{
    public CtAnnotatesNode? Annotations { get; }
    public NamedTypeSpacePrefixNode? NamedTypePrefix { get; }
    public string Name { get; }
    public TypeNode? Type { get; }
    public ExprNode? Initializer { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public StaticDeclNode(CtAnnotatesNode? annotations, NamedTypeSpacePrefixNode? namedTypePrefix, string name, TypeNode? type, ExprNode? initializer, TextSpan span, string fullText)
    {
        Annotations = annotations;
        NamedTypePrefix = namedTypePrefix;
        Name = name;
        Type = type;
        Initializer = initializer;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.StaticDeclNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Annotations != null)
            yield return Annotations;
        if (NamedTypePrefix != null)
            yield return NamedTypePrefix;
        if (Type != null)
            yield return Type;
        if (Initializer != null)
            yield return Initializer;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}

/// <summary>
/// Represents a variable declaration: var name: type = expr;
/// </summary>
public sealed class VarDeclNode : DeclNode
{
    public CtAnnotatesNode? Annotations { get; }
    public string Name { get; }
    public TypeNode? Type { get; }
    public ExprNode Initializer { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public VarDeclNode(CtAnnotatesNode? annotations, string name, TypeNode? type, ExprNode initializer, TextSpan span, string fullText)
    {
        Annotations = annotations;
        Name = name;
        Type = type;
        Initializer = initializer;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.VarDeclNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Annotations != null)
            yield return Annotations;
        if (Type != null)
            yield return Type;
        yield return Initializer;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
