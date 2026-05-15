namespace SolidLang.Parser.Nodes.Types;

/// <summary>
/// Represents type arguments: &lt;T1, T2, ...&gt;
/// </summary>
public sealed class TypeArgumentListNode : SyntaxNode
{
    public IReadOnlyList<TypeNode> Arguments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public TypeArgumentListNode(IReadOnlyList<TypeNode> arguments, TextSpan span, string fullText)
    {
        Arguments = arguments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.TypeArgumentListNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Arguments;

    public override string GetFullText() => _fullText;
}
