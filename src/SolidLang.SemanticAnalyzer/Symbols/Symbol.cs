using SolidLang.Parser.Nodes;

namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Abstract base for all symbols in the semantic model.
/// A symbol represents a named entity: type, variable, function, member, etc.
/// </summary>
public abstract class Symbol
{
    public abstract SymbolKind Kind { get; }
    public abstract string Name { get; }
    public abstract SyntaxNode? Declaration { get; internal set; }

    /// <summary>
    /// The scope that owns this symbol.
    /// Set by the SymbolBuilderPass when the symbol is registered.
    /// </summary>
    public Scope? ContainingScope { get; internal set; }
}
