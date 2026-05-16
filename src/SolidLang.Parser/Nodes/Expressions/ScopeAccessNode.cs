namespace SolidLang.Parser.Nodes.Expressions;

/// <summary>
/// Represents a scope access: ::name or ::name(args)
/// </summary>
public sealed class ScopeAccessNode : PostfixSuffixNode
{
    public string Name { get; }
    public IReadOnlyList<CallArgNode> Arguments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ScopeAccessNode(string name, IReadOnlyList<CallArgNode> arguments, TextSpan span, string fullText)
    {
        Name = name;
        Arguments = arguments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ScopeAccessNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var a in Arguments)
            yield return a;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
