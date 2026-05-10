using SolidLang.Parser.Nodes;

namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Represents a type declaration: struct, enum, union, variant, interface, or built-in primitive.
/// </summary>
public sealed class TypeSymbol : Symbol
{
    public override SymbolKind Kind { get; }
    public override string Name { get; }
    public override SyntaxNode? Declaration { get; }

    /// <summary>
    /// Whether this is a forward declaration awaiting a full definition.
    /// </summary>
    public bool IsForwardDecl { get; internal set; }

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
    public TypeSymbol(SymbolKind kind, string name, SyntaxNode declaration, bool isForwardDecl,
        IReadOnlyList<GenericParamSymbol>? genericParams = null, Scope? typeScope = null)
    {
        Kind = kind;
        Name = name;
        Declaration = declaration;
        IsForwardDecl = isForwardDecl;
        GenericParams = genericParams ?? Array.Empty<GenericParamSymbol>();
        TypeScope = typeScope;

        // If a type scope is provided, set its owning node
        if (typeScope is Scope s)
            s.OwningTypeSymbol = this;
    }

    /// <summary>
    /// Factory for built-in primitive types (i8, bool, etc.).
    /// </summary>
    public static TypeSymbol CreatePrimitive(string name)
    {
        return new TypeSymbol(SymbolKind.Primitive, name, null!, false, null, null);
    }
}
