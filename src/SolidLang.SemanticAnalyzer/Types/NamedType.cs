namespace SolidLang.SemanticAnalyzer;

/// <summary>
/// Represents a reference to a user-defined type, possibly with type arguments (e.g., Vector&lt;i32&gt;).
/// </summary>
public sealed class NamedType : SolidType
{
    /// <summary>
    /// The resolved type symbol that this named type refers to.
    /// </summary>
    public TypeSymbol TypeSymbol { get; }

    /// <summary>
    /// The type arguments, if any. Empty for non-generic types.
    /// </summary>
    public IReadOnlyList<SolidType> TypeArguments { get; }

    public override SolidTypeKind Kind => SolidTypeKind.Named;
    public override string DisplayName =>
        TypeArguments.Count > 0
            ? $"{TypeSymbol.Name}<{string.Join(", ", TypeArguments.Select(t => t.DisplayName))}>"
            : TypeSymbol.Name;

    public bool IsGeneric => TypeSymbol.GenericParams.Count > 0;

    /// <summary>
    /// Whether all type arguments are concrete (no TypeParameters remain).
    /// </summary>
    public bool IsConcrete => TypeArguments.All(a => a.Kind != SolidTypeKind.TypeParameter);

    public NamedType(TypeSymbol typeSymbol, IReadOnlyList<SolidType>? typeArguments = null)
    {
        TypeSymbol = typeSymbol;
        TypeArguments = typeArguments ?? Array.Empty<SolidType>();
    }

    /// <summary>
    /// Creates a NamedType for a non-generic type.
    /// </summary>
    public static NamedType From(TypeSymbol symbol)
        => new(symbol, Array.Empty<SolidType>());

    public override bool Equals(object? obj)
    {
        if (obj is not NamedType other) return false;
        if (other.TypeSymbol != TypeSymbol) return false;
        if (other.TypeArguments.Count != TypeArguments.Count) return false;
        for (int i = 0; i < TypeArguments.Count; i++)
            if (!other.TypeArguments[i].Equals(TypeArguments[i])) return false;
        return true;
    }

    public override int GetHashCode()
    {
        var hc = new HashCode();
        hc.Add(TypeSymbol);
        foreach (var a in TypeArguments) hc.Add(a);
        return hc.ToHashCode();
    }
}
