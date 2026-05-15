namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents function parameters: (param1: type1, param2: type2)
/// </summary>
public sealed class FuncParametersNode : SyntaxNode
{
    public IReadOnlyList<FuncParameterNode> Parameters { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public FuncParametersNode(IReadOnlyList<FuncParameterNode> parameters, TextSpan span, string fullText)
    {
        Parameters = parameters;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.FuncParametersNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Parameters;

    public override string GetFullText() => _fullText;
}
