namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents a calling convention: cdecl or stdcall
/// </summary>
public sealed class CallConventionNode : SyntaxNode
{
    public SyntaxKind Convention { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CallConventionNode(SyntaxKind convention, TextSpan span, string fullText)
    {
        Convention = convention;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CallConventionNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Convention}]");
    }
}
