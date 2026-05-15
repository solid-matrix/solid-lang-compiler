using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents a named-type prefix for out-of-line declarations: Type&lt;T&gt;::
/// Each segment is a NamedTypeNode (which may contain its own namespace prefix + generics).
/// </summary>
public sealed class NamedTypeSpacePrefixNode : SyntaxNode
{
    public Types.NamedTypeNode NamedType { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public NamedTypeSpacePrefixNode(Types.NamedTypeNode namedType, TextSpan span, string fullText)
    {
        NamedType = namedType;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.NamedTypeSpacePrefixNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return NamedType;
    }

    public override string GetFullText() => _fullText;
}
