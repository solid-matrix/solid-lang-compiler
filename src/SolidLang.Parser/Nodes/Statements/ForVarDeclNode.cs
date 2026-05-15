using SolidLang.Parser.Nodes.Expressions;

namespace SolidLang.Parser.Nodes.Statements;

/// <summary>
/// Represents a for loop variable declaration: var name = expr
/// </summary>
public sealed class ForVarDeclNode : ForInitNode
{
    public Declarations.CtAnnotatesNode? Annotations { get; }
    public string Name { get; }
    public ExprNode Initializer { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ForVarDeclNode(Declarations.CtAnnotatesNode? annotations, string name, ExprNode initializer, TextSpan span, string fullText)
    {
        Annotations = annotations;
        Name = name;
        Initializer = initializer;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ForVarDeclNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Annotations != null)
            yield return Annotations;
        yield return Initializer;
    }

    public override string GetFullText() => _fullText;
}
