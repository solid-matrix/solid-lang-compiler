using SolidLang.Parser.Nodes;

namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Represents a variable declaration: var, const, static, function parameter, or for-loop variable.
/// </summary>
public sealed class VariableSymbol : Symbol
{
    public override SymbolKind Kind { get; }
    public override string Name { get; }
    public override SyntaxNode Declaration { get; }

    /// <summary>
    /// The declared type from the : type annotation, or null if type-inferred.
    /// </summary>
    public SolidType? DeclaredType { get; }

    public bool IsReadOnly => Kind is SymbolKind.ConstVariable or SymbolKind.Parameter;
    public bool IsStatic => Kind == SymbolKind.StaticVariable;
    public bool IsParameter => Kind == SymbolKind.Parameter;
    public bool IsForLoopVar => Kind == SymbolKind.ForLoopVariable;

    public VariableSymbol(SymbolKind kind, string name, SyntaxNode declaration, SolidType? declaredType = null)
    {
        Kind = kind;
        Name = name;
        Declaration = declaration;
        DeclaredType = declaredType;
    }
}
