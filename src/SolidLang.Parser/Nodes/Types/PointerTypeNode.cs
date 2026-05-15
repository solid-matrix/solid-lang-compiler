namespace SolidLang.Parser.Nodes.Types;

/// <summary>
/// Represents a pointer type: *type or *!type
/// </summary>
public sealed class PointerTypeNode : TypeNode
{
    public bool HasWritePermission { get; }
    public TypeNode PointeeType { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public PointerTypeNode(bool hasWritePermission, TypeNode pointeeType, TextSpan span, string fullText)
    {
        HasWritePermission = hasWritePermission;
        PointeeType = pointeeType;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.PointerTypeNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return PointeeType;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        if (HasWritePermission)
            writer.Write(" [mutable]");
    }
}
