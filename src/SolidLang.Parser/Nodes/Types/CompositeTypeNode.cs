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
