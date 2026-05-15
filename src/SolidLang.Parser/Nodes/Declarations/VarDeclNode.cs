using SolidLang.Parser.Nodes.Expressions;
using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents a variable declaration: var name: type = expr;
/// </summary>
public sealed class VarDeclNode : DeclNode
{
    public CtAnnotatesNode? Annotations { get; }
    public string Name { get; }
    public TypeNode? Type { get; }
    public ExprNode Initializer { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public VarDeclNode(CtAnnotatesNode? annotations, string name, TypeNode? type, ExprNode initializer, TextSpan span, string fullText)
    {
        Annotations = annotations;
        Name = name;
        Type = type;
        Initializer = initializer;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.VarDeclNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Annotations != null)
            yield return Annotations;
        if (Type != null)
            yield return Type;
        yield return Initializer;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
