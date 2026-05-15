namespace SolidLang.Parser.Nodes;

/// <summary>
/// Represents a bad declaration (used for error recovery).
/// </summary>
public sealed class BadDeclNode : Declarations.DeclNode
{
    private readonly TextSpan _span;
    private readonly string _fullText;

    public BadDeclNode(TextSpan span = default, string fullText = "")
    {
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.BadDeclNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => _fullText;
}
