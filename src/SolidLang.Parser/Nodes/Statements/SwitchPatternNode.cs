using SolidLang.Parser.Nodes.Expressions;
using SolidLang.Parser.Nodes.Literals;

namespace SolidLang.Parser.Nodes.Statements;

/// <summary>
/// Represents a switch pattern.
/// </summary>
public sealed class SwitchPatternNode : SyntaxNode
{
    public SwitchPatternKind PatternKind { get; }
    public LiteralNode? Literal { get; }
    public Types.NamedTypeNode? NamedType { get; }
    public string? MemberName { get; }
    public ExprNode? Binding { get; }
    public string? Identifier { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public SwitchPatternNode(
        SwitchPatternKind kind,
        LiteralNode? literal,
        Types.NamedTypeNode? namedType,
        string? memberName,
        ExprNode? binding,
        string? identifier,
        TextSpan span,
        string fullText)
    {
        PatternKind = kind;
        Literal = literal;
        NamedType = namedType;
        MemberName = memberName;
        Binding = binding;
        Identifier = identifier;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.SwitchPatternNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Literal != null)
            yield return Literal;
        if (NamedType != null)
            yield return NamedType;
        if (Binding != null)
            yield return Binding;
    }

    public override string GetFullText() => _fullText;
}
