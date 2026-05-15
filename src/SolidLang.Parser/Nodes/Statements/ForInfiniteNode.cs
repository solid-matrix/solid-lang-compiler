namespace SolidLang.Parser.Nodes.Statements;

/// <summary>
/// Represents an infinite for loop: for { ... }
/// </summary>
public sealed class ForInfiniteNode : ForKindNode
{
    public BodyStmtNode Body { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ForInfiniteNode(BodyStmtNode body, TextSpan span, string fullText)
    {
        Body = body;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ForInfiniteNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Body;
    }

    public override string GetFullText() => _fullText;
}
