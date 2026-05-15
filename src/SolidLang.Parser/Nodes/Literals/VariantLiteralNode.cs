using SolidLang.Parser.Nodes.Expressions;
using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Literals;

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
