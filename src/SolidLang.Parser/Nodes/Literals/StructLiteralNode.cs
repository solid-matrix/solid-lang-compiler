using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Literals;

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
