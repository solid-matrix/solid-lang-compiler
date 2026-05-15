using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Expressions;

/// <summary>
/// Represents a dot access: .name with optional generic type arguments (.name&lt;T&gt;)
/// </summary>
public sealed class DotAccessNode : PostfixSuffixNode
{
    public string Name { get; }
    public TypeArgumentListNode? TypeArguments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public DotAccessNode(string name, TypeArgumentListNode? typeArguments, TextSpan span, string fullText)
    {
        Name = name;
        TypeArguments = typeArguments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.DotAccessNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (TypeArguments != null)
            yield return TypeArguments;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
