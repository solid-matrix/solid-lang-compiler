using SolidLang.Parser.Nodes.Expressions;

namespace SolidLang.Parser.Nodes.Declarations;

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
