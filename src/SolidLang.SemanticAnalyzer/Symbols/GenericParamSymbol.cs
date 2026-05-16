using SolidLang.Parser.Nodes;
using SolidLang.Parser.Nodes.Declarations;

namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Represents a generic type parameter (e.g., T in struct Vector&lt;T&gt;).
/// </summary>
public sealed class GenericParamSymbol : Symbol
{
    public override SymbolKind Kind => SymbolKind.GenericParam;
    public override string Name { get; }
    public override SyntaxNode Declaration { get; internal set; }

    public GenericParamSymbol(string name, GenericParamNode declaration)
    {
        Name = name;
        Declaration = declaration;
    }
}
