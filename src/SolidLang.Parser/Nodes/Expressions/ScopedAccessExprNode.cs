using SolidLang.Parser.Nodes.Declarations;
using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Expressions;

/// <summary>
/// Represents a scoped access expression: [NS::][Type&lt;T&gt;::]member or [NS::][Type&lt;T&gt;::]member(args)
/// The prefix (if any) is a NamedTypeSpacePrefixNode containing the full ::-chain before the member.
/// TypeArguments provides generic args when no :: prefix is present (e.g., identity&lt;i32&gt;(42)).
/// </summary>
public sealed class ScopedAccessExprNode : ExprNode
{
    public NamedTypeSpacePrefixNode? Prefix { get; }
    public string Name { get; }
    public CallArgsNode? Arguments { get; }
    public TypeArgumentListNode? TypeArguments { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ScopedAccessExprNode(NamedTypeSpacePrefixNode? prefix, string name, CallArgsNode? arguments, TypeArgumentListNode? typeArguments, TextSpan span, string fullText)
    {
        Prefix = prefix;
        Name = name;
        Arguments = arguments;
        TypeArguments = typeArguments;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ScopedAccessExprNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Prefix != null)
            yield return Prefix;
        if (TypeArguments != null)
            yield return TypeArguments;
        if (Arguments != null)
            yield return Arguments;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
