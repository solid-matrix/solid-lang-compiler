namespace SolidLang.Parser.Nodes.Literals;

/// <summary>
/// Represents an integer literal.
/// </summary>
public sealed class IntegerLiteralNode : LiteralNode
{
    public string Text { get; }
    public object Value { get; }
    public SyntaxKind? TypeSuffix { get; }
    private readonly TextSpan _span;

    public IntegerLiteralNode(string text, object value, SyntaxKind? typeSuffix, TextSpan span)
    {
        Text = text;
        Value = value;
        TypeSuffix = typeSuffix;
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.IntegerLiteralNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => Text;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Value}]");
    }
}

/// <summary>
/// Represents a float literal.
/// </summary>
public sealed class FloatLiteralNode : LiteralNode
{
    public string Text { get; }
    public object Value { get; }
    public SyntaxKind? TypeSuffix { get; }
    private readonly TextSpan _span;

    public FloatLiteralNode(string text, object value, SyntaxKind? typeSuffix, TextSpan span)
    {
        Text = text;
        Value = value;
        TypeSuffix = typeSuffix;
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.FloatLiteralNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => Text;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Value}]");
    }
}

/// <summary>
/// Represents a string literal.
/// </summary>
public sealed class StringLiteralNode : LiteralNode
{
    public string Text { get; }
    public string Value { get; }
    private readonly TextSpan _span;

    public StringLiteralNode(string text, string value, TextSpan span)
    {
        Text = text;
        Value = value;
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.StringLiteralNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => Text;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [\"{Value}\"]");
    }
}

/// <summary>
/// Represents a character literal.
/// </summary>
public sealed class CharLiteralNode : LiteralNode
{
    public string Text { get; }
    public string Value { get; }
    private readonly TextSpan _span;

    public CharLiteralNode(string text, string value, TextSpan span)
    {
        Text = text;
        Value = value;
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.CharLiteralNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => Text;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" ['{Value}']");
    }
}

/// <summary>
/// Represents a boolean literal: true or false.
/// </summary>
public sealed class BoolLiteralNode : LiteralNode
{
    public bool Value { get; }
    private readonly TextSpan _span;

    public BoolLiteralNode(bool value, TextSpan span)
    {
        Value = value;
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.BoolLiteralNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => Value ? "true" : "false";

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Value}]");
    }
}

/// <summary>
/// Represents a null literal.
/// </summary>
public sealed class NullLiteralNode : LiteralNode
{
    private readonly TextSpan _span;

    public NullLiteralNode(TextSpan span)
    {
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.NullLiteralNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => "null";
}
