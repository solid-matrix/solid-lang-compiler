using SolidLang.Parser.Nodes.Expressions;

namespace SolidLang.Parser.Nodes.Statements;

/// <summary>
/// Represents a for statement: for (forms)
/// </summary>
public sealed class ForStmtNode : StmtNode
{
    public ForKindNode KindNode { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ForStmtNode(ForKindNode kindNode, TextSpan span, string fullText)
    {
        KindNode = kindNode;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ForStmtNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return KindNode;
    }

    public override string GetFullText() => _fullText;
}
