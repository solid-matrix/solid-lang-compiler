using SolidLang.Parser.Nodes.Expressions;
using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Literals;

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
