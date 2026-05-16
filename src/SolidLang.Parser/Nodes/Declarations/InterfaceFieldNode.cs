using SolidLang.Parser.Nodes.Types;

namespace SolidLang.Parser.Nodes.Declarations;

/// <summary>
/// Represents an interface field (method signature): func name&lt;T&gt;(params): type
/// </summary>
public sealed class InterfaceFieldNode : SyntaxNode
{
    public IReadOnlyList<CtAnnotateNode> Annotations { get; }
    public string Name { get; }
    public IReadOnlyList<GenericParamNode> GenericParams { get; }
    public IReadOnlyList<FuncParameterNode> Parameters { get; }
    public Types.TypeNode? ReturnType { get; }
    private readonly TextSpan _span;
    private readonly string _fullText;

    public InterfaceFieldNode(
        IReadOnlyList<CtAnnotateNode> annotations,
        string name,
        IReadOnlyList<GenericParamNode> genericParams,
        IReadOnlyList<FuncParameterNode> parameters,
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
        foreach (var a in Annotations)
            yield return a;
        foreach (var p in GenericParams)
            yield return p;
        foreach (var p in Parameters)
            yield return p;
        if (ReturnType != null)
            yield return ReturnType;
    }

    public override string GetFullText() => _fullText;

    protected override void WriteAdditionalInfo(TextWriter writer)
    {
        writer.Write($" [{Name}]");
    }
}
