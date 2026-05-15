using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents a where clause: where T: Trait
/// </summary>
public sealed class WhereClauseNode : SyntaxNode
{
    public string TypeParamName { get; }
    public Types.TypeNode ConstraintType { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public WhereClauseNode(string typeParamName, Types.TypeNode constraintType, TextSpan span, string fullText)
    {
        TypeParamName = typeParamName;
        ConstraintType = constraintType;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.WhereClauseNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return ConstraintType;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{TypeParamName}]");
    }
}
