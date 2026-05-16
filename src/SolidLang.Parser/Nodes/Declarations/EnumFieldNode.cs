using SolidLang.Parser.Nodes.Expressions;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents an enum field: Name = value or just Name
/// </summary>
public sealed class EnumFieldNode : SyntaxNode
{
    public IReadOnlyList<CtAnnotateNode> Annotations { get; }
    public string Name { get; }
    public ExprNode? Value { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public EnumFieldNode(IReadOnlyList<CtAnnotateNode> annotations, string name, ExprNode? value, TextSpan span, string fullText)
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
        foreach (var a in Annotations)
            yield return a;
        if (Value != null)
            yield return Value;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
