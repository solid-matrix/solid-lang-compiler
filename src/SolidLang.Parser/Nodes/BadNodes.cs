using SolidLang.Parser.Nodes.Expressions;

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

/// <summary>
/// Represents a bad statement (used for error recovery).
/// </summary>
public sealed class BadStmtNode : Statements.StmtNode
{
    private readonly TextSpan _span;
    private readonly string _fullText;

    public BadStmtNode(TextSpan span = default, string fullText = "")
    {
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.BadStmtNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents a bad expression (used for error recovery).
/// </summary>
public sealed class BadExprNode : ExprNode
{
    private readonly TextSpan _span;
    private readonly string _fullText;

    public BadExprNode(TextSpan span = default, string fullText = "")
    {
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.BadExprNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents a bad type (used for error recovery).
/// </summary>
public sealed class BadTypeNode : Types.TypeNode
{
    private readonly TextSpan _span;
    private readonly string _fullText;

    public BadTypeNode(TextSpan span = default, string fullText = "")
    {
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.BadTypeNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => _fullText;
}
