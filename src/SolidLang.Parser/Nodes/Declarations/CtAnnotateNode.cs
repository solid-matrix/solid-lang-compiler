namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents a compile-time annotation: @name or @name(args)
/// </summary>
public sealed class CtAnnotateNode : SyntaxNode
{
    public string Name { get; }
    public CtAnnotateArgsNode? Arguments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public CtAnnotateNode(string name, CtAnnotateArgsNode? arguments, TextSpan span, string fullText)
    {
        Name = name;
        Arguments = arguments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.CtAnnotateNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Arguments != null)
            yield return Arguments;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
