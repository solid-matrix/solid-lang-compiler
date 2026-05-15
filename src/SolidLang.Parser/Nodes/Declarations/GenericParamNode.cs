namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents a generic parameter: T
/// </summary>
public sealed class GenericParamNode : SyntaxNode
{
    public string Name { get; }
    private readonly TextSpan _span;

    public GenericParamNode(string name, TextSpan span)
    {
        Name = name;
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.GenericParamNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => Name;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
