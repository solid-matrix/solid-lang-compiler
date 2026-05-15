namespace SolidLang.Parser.Nodes.Types;

/// <summary>
/// Represents an integer type: i8, i16, i32, i64, isize, u8, u16, u32, u64, usize
/// </summary>
public sealed class IntegerTypeNode : TypeNode
{
    public SyntaxKind IntegerKind { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public IntegerTypeNode(SyntaxKind integerKind, TextSpan span, string fullText)
    {
        IntegerKind = integerKind;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.IntegerTypeNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{IntegerKind}]");
    }
}
