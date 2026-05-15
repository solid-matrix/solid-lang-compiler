using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents an interface field (method signature): func name&lt;T&gt;(params): type
/// </summary>
public sealed class InterfaceFieldNode : SyntaxNode
{
    public CtAnnotatesNode? Annotations { get; }
    public string Name { get; }
    public GenericParamsNode? GenericParams { get; }
    public FuncParametersNode? Parameters { get; }
    public Types.TypeNode? ReturnType { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public InterfaceFieldNode(
        CtAnnotatesNode? annotations,
        string name,
        GenericParamsNode? genericParams,
        FuncParametersNode? parameters,
        Types.TypeNode? returnType,
        TextSpan span,
        string fullText)
    {
        Annotations = annotations;
        Name = name;
        GenericParams = genericParams;
        Parameters = parameters;
        ReturnType = returnType;
        _span = span;
        _fullText = fullText;
    }

    public override SyntaxKind Kind => SyntaxKind.InterfaceFieldNode;
    public override TextSpan Span => _span;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        if (Annotations != null)
            yield return Annotations;
        if (GenericParams != null)
            yield return GenericParams;
        if (Parameters != null)
            yield return Parameters;
        if (ReturnType != null)
            yield return ReturnType;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
