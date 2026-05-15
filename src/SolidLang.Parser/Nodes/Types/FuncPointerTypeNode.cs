namespace SolidLang.Parser.Nodes.Types;

/// <summary>
/// Represents a function pointer type: *func(...) call_conv: type
/// </summary>
public sealed class FuncPointerTypeNode : TypeNode
{
    public IReadOnlyList<TypeNode> ParameterTypes { get; }
    public Declarations.CallConventionNode? CallingConvention { get; }
    public TypeNode ReturnType { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public FuncPointerTypeNode(
        IReadOnlyList<TypeNode> parameterTypes,
        Declarations.CallConventionNode? callingConvention,
        TypeNode returnType,
        TextSpan span,
        string fullText)
    {
        ParameterTypes = parameterTypes;
        CallingConvention = callingConvention;
        ReturnType = returnType;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.FuncPointerTypeNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var p in ParameterTypes)
            yield return p;
        if (CallingConvention != null)
            yield return CallingConvention;
        yield return ReturnType;
    }

    public override string GetFullText() => _fullText;
}
