using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Literals;

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
