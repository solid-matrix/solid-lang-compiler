namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents generic parameters: &lt;T, U, V&gt;
/// </summary>
public sealed class GenericParamsNode : SyntaxNode
{
    public IReadOnlyList<GenericParamNode> Parameters { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public GenericParamsNode(IReadOnlyList<GenericParamNode> parameters, TextSpan span, string fullText)
    {
        Parameters = parameters;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.GenericParamsNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Parameters;

    public override string GetFullText() => _fullText;
}
