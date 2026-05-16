using SolidLang.Parser.Nodes;

namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Represents a type declaration: struct, enum, union, variant, interface, or built-in primitive.
/// </summary>
public sealed class TypeSymbol : Symbol
{
    public override SymbolKind Kind { get; }
    public override string Name { get; }
    public override SyntaxNode? Declaration { get; internal set; }

    /// <summary>
    /// The generic type parameters declared on this type, if any. Empty for non-generic types.
    /// </summary>
    public IReadOnlyList<GenericParamSymbol> GenericParams { get; }

    /// <summary>
    /// The scope containing this type's fields/methods. Null for primitive types.
    /// </summary>
    public Scope? TypeScope { get; internal set; }

    public bool IsPrimitive => Kind == SymbolKind.Primitive;

    /// <summary>
    /// Constructor for user-defined types.
    /// </summary>
    public TypeSymbol(SymbolKind kind, string name, SyntaxNode declaration,
        IReadOnlyList<GenericParamSymbol>? genericParams = null, Scope? typeScope = null)
    {
        Kind = kind;
        Name = name;
        Declaration = declaration;
        GenericParams = genericParams ?? Array.Empty<GenericParamSymbol>();
        TypeScope = typeScope;

        // If a type scope is provided, set its owning node
        if (typeScope is Scope s)
            s.OwningTypeSymbol = this;
    }

    internal void SetDeclaration(SyntaxNode node) => Declaration = node;

    /// <summary>
    /// Factory for built-in primitive types (i8, bool, etc.).
    /// </summary>
    public static TypeSymbol CreatePrimitive(string name)
    {
        return new TypeSymbol(SymbolKind.Primitive, name, null!, null, null);
    }
}
