using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents a function parameter: name: type
/// </summary>
public sealed class FuncParameterNode : SyntaxNode
{
    public IReadOnlyList<CtAnnotateNode> Annotations { get; }
    public string Name { get; }
    public Types.TypeNode Type { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public FuncParameterNode(IReadOnlyList<CtAnnotateNode> annotations, string name, Types.TypeNode type, TextSpan span, string fullText)
    {
        Annotations = annotations;
        Name = name;
        Type = type;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.FuncParameterNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var a in Annotations)
            yield return a;
        yield return Type;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
