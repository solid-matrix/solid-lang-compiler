namespace SolidLang.Parser.Nodes.Types;

/// <summary>
/// Represents a primitive type: i32, f64, bool, etc.
/// </summary>
public sealed class PrimitiveTypeNode : TypeNode
{
    public SyntaxKind PrimitiveKind { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public PrimitiveTypeNode(SyntaxKind primitiveKind, TextSpan span, string fullText)
    {
        PrimitiveKind = primitiveKind;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.PrimitiveTypeNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{PrimitiveKind}]");
    }
}

/// <summary>
/// Represents an integer type: i8, i16, i32, i64, isize, u8, u16, u32, u64, usize
/// </summary>
public sealed class IntegerTypeNode : TypeNode
{
    public SyntaxKind IntegerKind { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public IntegerTypeNode(SyntaxKind integerKind, TextSpan span, string fullText)
    {
        IntegerKind = integerKind;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.IntegerTypeNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{IntegerKind}]");
    }
}

/// <summary>
/// Represents a float type: f32, f64
/// </summary>
public sealed class FloatTypeNode : TypeNode
{
    public SyntaxKind FloatKind { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public FloatTypeNode(SyntaxKind floatKind, TextSpan span, string fullText)
    {
        FloatKind = floatKind;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.FloatTypeNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{FloatKind}]");
    }
}

/// <summary>
/// Represents the bool type.
/// </summary>
public sealed class BoolTypeNode : TypeNode
{
    private readonly TextSpan _span;

    public BoolTypeNode(TextSpan span)
    {
        _span = span;
    }

    public override SyntaxKind Kind => SyntaxKind.BoolTypeNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren() => Enumerable.Empty<SyntaxNode>();

    public override string GetFullText() => "bool";
}
