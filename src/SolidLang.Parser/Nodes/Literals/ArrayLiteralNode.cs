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
