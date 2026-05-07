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

/// <summary>
/// Base class for different for loop forms.
/// </summary>
public abstract class ForKindNode : SyntaxNode
{
}

/// <summary>
/// Represents an infinite for loop: for { ... }
/// </summary>
public sealed class ForInfiniteNode : ForKindNode
{
    public BodyStmtNode Body { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ForInfiniteNode(BodyStmtNode body, TextSpan span, string fullText)
    {
        Body = body;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ForInfiniteNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Body;
    }

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents a conditional for loop (while-style): for cond { ... }
/// </summary>
public sealed class ForCondNode : ForKindNode
{
    public ExprNode Condition { get; }
    public BodyStmtNode Body { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ForCondNode(ExprNode condition, BodyStmtNode body, TextSpan span, string fullText)
    {
        Condition = condition;
        Body = body;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ForCondNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Condition;
        yield return Body;
    }

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents a C-style for loop: for init; cond; update { ... }
/// </summary>
public sealed class ForCStyleNode : ForKindNode
{
    public ForInitNode? Init { get; }
    public ExprNode? Condition { get; }
    public ExprNode? Update { get; }
    public BodyStmtNode Body { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ForCStyleNode(ForInitNode? init, ExprNode? condition, ExprNode? update, BodyStmtNode body, TextSpan span, string fullText)
    {
        Init = init;
        Condition = condition;
        Update = update;
        Body = body;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ForCStyleNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Init != null)
            yield return Init;
        if (Condition != null)
            yield return Condition;
        if (Update != null)
            yield return Update;
        yield return Body;
    }

    public override string GetFullText() => _fullText;
}

/// <summary>
/// Represents for loop initialization.
/// </summary>
public abstract class ForInitNode : SyntaxNode
{
}

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

/// <summary>
/// Represents a for loop assignment: expr = expr
/// </summary>
public sealed class ForAssignNode : ForInitNode
{
    public ExprNode Target { get; }
    public SyntaxKind Operator { get; }
    public ExprNode Value { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public ForAssignNode(ExprNode target, SyntaxKind op, ExprNode value, TextSpan span, string fullText)
    {
        Target = target;
        Operator = op;
        Value = value;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.ForAssignNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Target;
        yield return Value;
    }

    public override string GetFullText() => _fullText;
}
