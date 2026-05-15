namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents where clauses: where T: Trait where U: AnotherTrait
/// </summary>
public sealed class WhereClausesNode : SyntaxNode
{
    public IReadOnlyList<WhereClauseNode> Clauses { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public WhereClausesNode(IReadOnlyList<WhereClauseNode> clauses, TextSpan span, string fullText)
    {
        Clauses = clauses;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.WhereClausesNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Clauses;

    public override string GetFullText() => _fullText;
}
