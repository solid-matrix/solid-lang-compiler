using SolidLang.Parser.Nodes;

namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Represents a namespace declaration.
/// </summary>
public sealed class NamespaceSymbol : Symbol
{
    public override SymbolKind Kind => SymbolKind.Namespace;
    public override string Name { get; }
    public override SyntaxNode Declaration { get; internal set; }

    /// <summary>
    /// The scope containing all members of this namespace.
    /// </summary>
    public Scope NamespaceScope { get; }

    /// <summary>
    /// The full qualified path, e.g., "std::math".
    /// </summary>
    public string FullPath { get; }

    public NamespaceSymbol(string name, SyntaxNode declaration, Scope namespaceScope, string fullPath)
    {
        Name = name;
        Declaration = declaration;
        NamespaceScope = namespaceScope;
        FullPath = fullPath;
    }
}
