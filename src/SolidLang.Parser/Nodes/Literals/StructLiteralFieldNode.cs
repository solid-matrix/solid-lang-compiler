using SolidLang.Parser.Nodes.Expressions;

namespace SolidLang.Parser.Nodes.Literals;

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
