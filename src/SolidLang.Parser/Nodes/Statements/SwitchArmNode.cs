namespace SolidLang.Parser.Nodes.Statements;

/// <summary>
/// Represents a switch arm: patterns... => stmt or else => stmt
/// </summary>
public sealed class SwitchArmNode : SyntaxNode
{
    public bool IsElse { get; }
    public IReadOnlyList<SwitchPatternNode> Patterns { get; }
    public StmtNode Statement { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public SwitchArmNode(bool isElse, IReadOnlyList<SwitchPatternNode> patterns, StmtNode statement, TextSpan span, string fullText)
    {
        IsElse = isElse;
        Patterns = patterns;
        Statement = statement;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.SwitchArmNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var p in Patterns)
            yield return p;
        yield return Statement;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        if (IsElse)
            writer.Write(" [else]");
    }
}
