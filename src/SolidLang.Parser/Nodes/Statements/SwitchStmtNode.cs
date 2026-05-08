using SolidLang.Parser.Nodes.Expressions;
using SolidLang.Parser.Nodes.Literals;

namespace SolidLang.Parser.Nodes.Statements;

/// <summary>
/// Represents a switch statement: switch expr { arms }
/// </summary>
public sealed class SwitchStmtNode : StmtNode
{
    public ExprNode Expression { get; }
    public IReadOnlyList<SwitchArmNode> Arms { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public SwitchStmtNode(ExprNode expression, IReadOnlyList<SwitchArmNode> arms, TextSpan span, string fullText)
    {
        Expression = expression;
        Arms = arms;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.SwitchStmtNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
        foreach (var arm in Arms)
            yield return arm;
    }

    public override string GetFullText() => _fullText;
}

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

/// <summary>
/// Represents a switch pattern.
/// </summary>
public sealed class SwitchPatternNode : SyntaxNode
{
    public SwitchPatternKind PatternKind { get; }
    public LiteralNode? Literal { get; }
    public Types.NamedTypeNode? NamedType { get; }
    public string? MemberName { get; }
    public ExprNode? Binding { get; }
    public string? Identifier { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public SwitchPatternNode(
        SwitchPatternKind kind,
        LiteralNode? literal,
        Types.NamedTypeNode? namedType,
        string? memberName,
        ExprNode? binding,
        string? identifier,
        TextSpan span,
        string fullText)
    {
        PatternKind = kind;
        Literal = literal;
        NamedType = namedType;
        MemberName = memberName;
        Binding = binding;
        Identifier = identifier;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.SwitchPatternNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Literal != null)
            yield return Literal;
        if (NamedType != null)
            yield return NamedType;
        if (Binding != null)
            yield return Binding;
    }

    public override string GetFullText() => _fullText;
}

public enum SwitchPatternKind
{
    Literal,
    NamedTypeMember,
    NamedTypeMemberBinding,
    Identifier,
}

/// <summary>
/// Represents break statement: break;
/// </summary>
public sealed class BreakStmtNode : StmtNode
{
    private readonly TextSpan _span;

    public BreakStmtNode(TextSpan span)
    {
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.BreakStmtNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => "break;";
}

/// <summary>
/// Represents continue statement: continue;
/// </summary>
public sealed class ContinueStmtNode : StmtNode
{
    private readonly TextSpan _span;

    public ContinueStmtNode(TextSpan span)
    {
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.ContinueStmtNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => "continue;";
}

/// <summary>
/// Represents return statement: return expr? ;
/// </summary>
public sealed class ReturnStmtNode : StmtNode
{
    public ExprNode? Expression { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ReturnStmtNode(ExprNode? expression, TextSpan span, string fullText)
    {
        Expression = expression;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ReturnStmtNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Expression != null)
            yield return Expression;
    }

    public override string GetFullText() => _fullText;
}
